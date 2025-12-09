using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RallyServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RallyController : ControllerBase
    {
        
        private readonly string rutaArchivo = "rally_data.json";

        // GET
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TramoRecord>>> GetTiempos()
        {
            
            if (!System.IO.File.Exists(rutaArchivo))
            {
                return new List<TramoRecord>();
            }

            string json = await System.IO.File.ReadAllTextAsync(rutaArchivo);
            var lista = JsonSerializer.Deserialize<List<TramoRecord>>(json);

           
            return lista.OrderBy(x => x.TiempoSegundos).ToList();
        }

        // GET: api/rally/estadisticas
        [HttpGet("estadisticas")]
        public async Task<ActionResult> GetEstadisticas()
        {
            if (!System.IO.File.Exists(rutaArchivo)) return Ok(new { mensaje = "Sin datos" });

            string json = await System.IO.File.ReadAllTextAsync(rutaArchivo);
            var lista = JsonSerializer.Deserialize<List<TramoRecord>>(json) ?? new List<TramoRecord>();

            if (lista.Count == 0) return Ok(new { mensaje = "Sin datos" });

            var estadisticas = new
            {
                TotalPilotos = lista.Count,
                TiempoMejor = lista.Min(x => x.TiempoSegundos),
                TiempoPeor = lista.Max(x => x.TiempoSegundos),
                TiempoPromedio = Math.Round(lista.Average(x => x.TiempoSegundos), 2),
                LiderActual = lista.OrderBy(x => x.TiempoSegundos).First().Piloto
            };

            return Ok(estadisticas);
        }

        // POST
        [HttpPost]
        public async Task<ActionResult> GuardarTiempo([FromBody] TramoRecord nuevoTiempo)
        {
          

           
            if (string.IsNullOrWhiteSpace(nuevoTiempo.Piloto) || string.IsNullOrWhiteSpace(nuevoTiempo.Coche))
            {
                return BadRequest("❌ Error: El nombre del piloto y el coche son obligatorios.");
            }

            if (nuevoTiempo.TiempoSegundos <= 0)
            {
                return BadRequest("❌ Error: El tiempo debe ser mayor que 0 segundos.");
            }

            List<TramoRecord> lista;
         
            string directorio = Path.GetDirectoryName(rutaArchivo);
            if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            if (System.IO.File.Exists(rutaArchivo))
            {
                string jsonLectura = await System.IO.File.ReadAllTextAsync(rutaArchivo);
                lista = JsonSerializer.Deserialize<List<TramoRecord>>(jsonLectura) ?? new List<TramoRecord>();
            }
            else
            {
                lista = new List<TramoRecord>();
            }

            if (lista.Any(x => x.Piloto.ToLower() == nuevoTiempo.Piloto.ToLower()))
            {
                return Conflict($"❌ Error: El piloto '{nuevoTiempo.Piloto}' ya ha registrado un tiempo.");
            }

            lista.Add(nuevoTiempo);

            string jsonEscritura = JsonSerializer.Serialize(lista);
            await System.IO.File.WriteAllTextAsync(rutaArchivo, jsonEscritura);

            return Ok(new { mensaje = "✅ Tiempo registrado correctamente" });

        }

        // PUT
        [HttpPut("{nombrePiloto}/penalizar")]
        public async Task<ActionResult> PenalizarPiloto(string nombrePiloto, [FromQuery] double segundosExtra)
        {
            if (!System.IO.File.Exists(rutaArchivo)) return NotFound();

            string jsonLectura = await System.IO.File.ReadAllTextAsync(rutaArchivo);
            var lista = JsonSerializer.Deserialize<List<TramoRecord>>(jsonLectura) ?? new List<TramoRecord>();

            var piloto = lista.FirstOrDefault(x => x.Piloto.ToLower() == nombrePiloto.ToLower());

            if (piloto == null) return NotFound("Piloto no encontrado");

            // Aplicar la penalización
            piloto.TiempoSegundos += segundosExtra;

            // Guardar cambios
            string jsonEscritura = JsonSerializer.Serialize(lista);
            await System.IO.File.WriteAllTextAsync(rutaArchivo, jsonEscritura);

            return Ok(new { mensaje = $"Se han añadido {segundosExtra}s de penalización a {piloto.Piloto}. Nuevo tiempo: {piloto.TiempoSegundos}" });
        }

        // DELETE
        [HttpDelete("{nombrePiloto}")]
        public async Task<ActionResult> BorrarTiempo(string nombrePiloto)
        {
            if (!System.IO.File.Exists(rutaArchivo))
            {
                return NotFound();
            }
            
            string jsonLectura = await System.IO.File.ReadAllTextAsync(rutaArchivo);
            var lista = JsonSerializer.Deserialize<List<TramoRecord>>(jsonLectura) ?? new List<TramoRecord>();

            
            var itemABorrar = lista.FirstOrDefault(x => x.Piloto.ToLower() == nombrePiloto.ToLower());

            if (itemABorrar == null)
            {
                return NotFound("Piloto no encontrado");
            }

            lista.Remove(itemABorrar);

           
            string jsonEscritura = JsonSerializer.Serialize(lista);
            await System.IO.File.WriteAllTextAsync(rutaArchivo, jsonEscritura);

            return Ok(new { mensaje = "Eliminado correctamente" });
        }

        // DELETE: api/rally/reset
        [HttpDelete("reset")]
        public ActionResult ResetTramo([FromHeader] string claveMaestra)
        {
            // Medida de seguridad simple (Header)
            if (claveMaestra != "DirectorCarrera123")
            {
                return Unauthorized("Acceso denegado. Se requiere clave de Director.");
            }

            if (System.IO.File.Exists(rutaArchivo))
            {
                System.IO.File.Delete(rutaArchivo); // Borra el archivo físico
                return Ok(new { mensaje = "⚠️ BASE DE DATOS REINICIADA. TRAMO LIMPIO." });
            }

            return Ok("Ya estaba limpio.");
        }


    }
}
