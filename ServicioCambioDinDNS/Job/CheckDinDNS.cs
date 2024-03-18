using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Quartz;
using ServicioCambioDinDNS.Tools;
using System.Text;
using System.Text.Json;

namespace ServicioCambioDinDNS.Job;

public class CheckDinDNS : IJob
{
    private readonly ILogger<CheckDinDNS> _logger;
	private readonly IConfiguration _configuration;
	private HttpClientHelper _HttpClientHelper;

	public CheckDinDNS(ILogger<CheckDinDNS> logger, ILogger<HttpClientHelper> httpClientLogger, IConfiguration configuration)
	{
		_logger = logger;
		_configuration = configuration;
		_HttpClientHelper = new HttpClientHelper(httpClientLogger, configuration);
	}

	public async Task Execute(IJobExecutionContext context)
    {
		var customer = Convert.ToString(_configuration["SubTask:CheckDinDNS:customer"]);
		var apiKey = Convert.ToString(_configuration["SubTask:CheckDinDNS:apikey"]);
		var xAPIKey = Convert.ToString(_configuration["SubTask:CheckDinDNS:x-api-key"]);
		List<string> listHostName = Convert.ToString(_configuration["SubTask:CheckDinDNS:hostname"] ?? "").Split(";").ToList();

		
		// Comienza la tarea imprime en consola
		Console.WriteLine("[Task:CheckDinDNS]: **************");
		Console.WriteLine("[Task:CheckDinDNS]: * Inicio Job *");
		Console.WriteLine("[Task:CheckDinDNS]: **************");

		// Dejamos un log guardado en el proyecto
		_logger.LogInformation("[Task:CheckDinDNS]: **************");
		_logger.LogInformation("[Task:CheckDinDNS]: * Inicio Job *");
		_logger.LogInformation("[Task:CheckDinDNS]: **************");

		var client = new HttpClient();

		// Procedemos a obtener la IP pública ( esto podria estar en un servicio por separado para tener un orden)
		#region[Paso 1 Obtener IP publica]

		// Entonces llenamos la variable con la IP_Publica que nos devuelve el servicio
		var IP_Publica = await _HttpClientHelper.SendHttpRequest(client, HttpMethod.Get, "https://api.ipify.org", null, null);
		if (IP_Publica == null)
		{
			_logger.LogError("La consulta a la IP publica no es posible en este momento");
			return;
		}

		Console.WriteLine(IP_Publica);
		#endregion

		//#region[Paso 2 Procesamos cada HostName]
		// Procesar cada hostName
		List<string> listUrl = new List<string>();
		if (listHostName != null && listHostName.Count() >= 1)
		{
			// Proceso de armar nuestro header
			var headers = new Dictionary<string, string>
			{
				{ "x-api-key", xAPIKey }
			};
			
			// Recorremos la lista de los N hostName
			foreach (var hostName in listHostName)
			{
				Console.WriteLine($"\n[Task:CheckDinDNS]: **************");
				Console.WriteLine($"\n[Task:CheckDinDNS]: * {hostName} *");
				Console.WriteLine($"\n[Task:CheckDinDNS]: **************");

				// Generamos el contenido de la solicitud
				var contentString = $"{{\r\n    \"url\": \"{hostName}\"\r\n}}";
				var content = new StringContent(contentString, Encoding.UTF8, "application/json");

				var responseContent = await _HttpClientHelper.SendHttpRequest(client, HttpMethod.Post, "https://api.siterelic.com/dnsrecord", headers, content);
				if (responseContent == null)
				{
					_logger.LogError("La consulta no es posible en este momento");
					return;
				}
				// Deserializamos el contenido de la respuesta
				JObject jsonResponse = JObject.Parse(responseContent);
				// Obtenemos el valor de address pasando por el nodo data -> A -> address que esta anidado
				var address = jsonResponse["data"]["A"][0]["address"]?.ToString() ?? "";

				//Console.WriteLine(responseContent);

				Console.WriteLine($"\n[Task:CheckDinDNS]: **************");
				Console.WriteLine($"\n[Task:CheckDinDNS]: * URL Obtenida {address} *");
				Console.WriteLine($"\n[Task:CheckDinDNS]: **************");

				// Si la IP publica es diferente a la IP que nos devuelve el servicio, entonces procedemos a actualizar el registro
				if (IP_Publica != address)
				{
					var url = $"https://{customer}:{apiKey}@members.dyndns.org/v3/update?hostname={hostName}&myip={IP_Publica}";
					var updateIp = await _HttpClientHelper.SendHttpRequest(client, HttpMethod.Get, url, null, null);
					if (updateIp != null)
					{
						Console.WriteLine($"\n[Task:CheckDinDNS]: **************");
						Console.WriteLine($"\n[Task:CheckDinDNS]: * La URL fue Actualizada *");
						Console.WriteLine($"\n[Task:CheckDinDNS]: **************");

						_logger.LogInformation("La URL fue actualizada");
					}
				}
				else {
					Console.WriteLine($"\n[Task:CheckDinDNS]: **************");
					Console.WriteLine($"\n[Task:CheckDinDNS]: * La IP Publica y la del hostName son las mismas *");
					Console.WriteLine($"\n[Task:CheckDinDNS]: **************");

					_logger.LogInformation("La IP Publica y la del hostName son las mismas ");
				}
			}
		}
		else
		{
			_logger.LogError("La configuración 'SubTask:CheckDinDNS:hostname' está vacía o no se encuentra.");
			return;
		}
		

		// Final de la tarea
		Console.WriteLine("Termina Job");
		await Task.CompletedTask;
	}
}
