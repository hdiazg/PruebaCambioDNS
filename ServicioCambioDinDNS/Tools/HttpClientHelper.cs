using ServicioCambioDinDNS.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServicioCambioDinDNS.Tools
{
	public  class HttpClientHelper
	{
		private readonly ILogger<HttpClientHelper> _logger;
		private readonly IConfiguration _configuration;

		public HttpClientHelper(ILogger<HttpClientHelper> logger, IConfiguration configuration)
		{
			_logger = logger;
			_configuration = configuration;
		}

		public async Task<string> SendHttpRequest(HttpClient client, HttpMethod method, string url, Dictionary<string, string>? headers, HttpContent? content)
		{
			try
			{
				using (var request = new HttpRequestMessage(method, url))
				{
					// Agregar los encabezados a la solicitud
					if (headers != null)
					{
						foreach (var header in headers)
						{
							request.Headers.Add(header.Key, header.Value);
						}
					}
					if (content != null) { 
						// Agregar el contenido a la solicitud
						request.Content = content;
					}
					
					var response = await client.SendAsync(request);
					//// Si pasamos este metodo, significa que la peticion fue exitosa
					response.EnsureSuccessStatusCode();
					return await response.Content.ReadAsStringAsync();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al enviar la solicitud HTTP");
				Console.WriteLine($"\n[Task:CheckDinDNS]: Ocurrio un error al enviar la solicitud HTTP");
				Console.WriteLine($"\n[Task:CheckDinDNS]: {ex.Message}");

				return null;
			}
		}

		public async Task<string> SendHttpRequestWithRetry(HttpClient client, HttpMethod method, string url, int retryCount = 3)
		{
			for (int i = 0; i < retryCount; i++)
			{
				try
				{
					using (var request = new HttpRequestMessage(method, url))
					{
						var response = await client.SendAsync(request);
						//// Si pasamos este metodo, significa que la peticion fue exitosa
						response.EnsureSuccessStatusCode();
						return await response.Content.ReadAsStringAsync();
					}
				}
				catch (Exception ex)
				{
					// Si es el último intento, lanzamos la excepción
					if (i == retryCount - 1)
					{
						_logger.LogError(ex, "Error al enviar la solicitud HTTP");
						Console.WriteLine($"\n[Task:CheckDinDNS]: Ocurrio un error al enviar la solicitud HTTP");
						Console.WriteLine($"\n[Task:CheckDinDNS]: {ex.Message}");
					}

					// Esperamos un poco antes de reintentar
					await Task.Delay(1000);
				}
			}

			return null; // Nunca deberíamos llegar a este punto
		}
	}
}
