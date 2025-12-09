using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RallyServer.Controllers
{

    public class TramoRecord
    {
        public string Piloto { get; set; }
        public string Coche { get; set; }
        public double TiempoSegundos { get; set; }
        public string Tramo { get; set; } = "SS1"; 
    }

    [Route("api/rally")]
    [ApiController]
    public class RallyController : ControllerBase
    {
        private readonly string rutaArchivo = "rally_data.json";

        // GET PILOTOS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TramoRecord>>> GetTiempos()
        {
            var lista = await LeerDatos();
            return lista.OrderBy(x => x.Tramo).ThenBy(x => x.TiempoSegundos).ToList();
        }

        // GET ANALITICAS 
        [HttpGet("estadisticas")]
        public async Task<ActionResult> GetEstadisticas()
        {
            var lista = await LeerDatos();
            if (lista.Count == 0) return Ok(new { mensaje = "Sin datos para analizar." });

            var estadisticas = new
            {
                TotalRegistros = lista.Count,
                MejorTiempoAbsoluto = lista.Min(x => x.TiempoSegundos),
                LiderDelRally = lista.OrderBy(x => x.TiempoSegundos).First().Piloto,
                TramosCorridos = lista.Select(x => x.Tramo).Distinct().ToList()
            };

            return Ok(estadisticas);
        }

        // POST VISUALIZAR 
        [HttpPost]
        public async Task<ActionResult> GuardarTiempo([FromBody] TramoRecord nuevo)
        {
            if (string.IsNullOrWhiteSpace(nuevo.Piloto) || string.IsNullOrWhiteSpace(nuevo.Coche))
                return BadRequest(new { mensaje = "Faltan datos." });

            if (nuevo.TiempoSegundos <= 0)
                return BadRequest(new { mensaje = "El tiempo debe ser positivo." });

            if (string.IsNullOrEmpty(nuevo.Tramo)) nuevo.Tramo = "SS1";

            var lista = await LeerDatos();
            var existente = lista.FirstOrDefault(x =>
                x.Piloto.ToLower() == nuevo.Piloto.ToLower() &&
                x.Tramo == nuevo.Tramo);

            if (existente != null)
            {

                return Conflict(new { mensaje = $" {nuevo.Piloto} ya tiene un tiempo registrado en {nuevo.Tramo}. No se puede duplicar." });
            }
            else
            {

                lista.Add(nuevo);
            }

            await GuardarDatos(lista);
            return Ok(new { mensaje = "Tiempo registrado correctamente." });
        }

        // PUT PENALIZACIONES 
        [HttpPut("{nombrePiloto}/penalizar")]
        public async Task<ActionResult> PenalizarPiloto(string nombrePiloto, [FromQuery] double segundos, [FromQuery] string tramo)
        {
            var lista = await LeerDatos();
            string tramoBusqueda = tramo ?? "SS1";

            var piloto = lista.FirstOrDefault(x =>
                x.Piloto.ToLower() == nombrePiloto.ToLower() &&
                x.Tramo == tramoBusqueda);

            if (piloto == null) return NotFound($"No se encontró a {nombrePiloto} en {tramoBusqueda}");

            piloto.TiempoSegundos += segundos; 

            await GuardarDatos(lista);
            return Ok(new { mensaje = $"Penalización de {segundos}s aplicada a {piloto.Piloto}. Nuevo tiempo: {piloto.TiempoSegundos}" });
        }

        // DELETE
        [HttpDelete("{nombrePiloto}")]
        public async Task<ActionResult> BorrarTiempo(string nombrePiloto, [FromQuery] string tramo)
        {
            var lista = await LeerDatos();
            string tramoBusqueda = tramo ?? "SS1";

            var item = lista.FirstOrDefault(x =>
                x.Piloto.ToLower() == nombrePiloto.ToLower() &&
                x.Tramo == tramoBusqueda);

            if (item == null) return NotFound("Registro no encontrado.");

            lista.Remove(item);
            await GuardarDatos(lista);
            return Ok(new { mensaje = "Eliminado correctamente." });
        }

        private async Task<List<TramoRecord>> LeerDatos()
        {
            if (!System.IO.File.Exists(rutaArchivo)) return new List<TramoRecord>();
            string json = await System.IO.File.ReadAllTextAsync(rutaArchivo);
            return JsonSerializer.Deserialize<List<TramoRecord>>(json) ?? new List<TramoRecord>();
        }

        private async Task GuardarDatos(List<TramoRecord> lista)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(lista, options);
            await System.IO.File.WriteAllTextAsync(rutaArchivo, json);
        }

        // RESET 
        [HttpDelete("reset")]
        public ActionResult Reset()
        {
            if (System.IO.File.Exists(rutaArchivo)) System.IO.File.Delete(rutaArchivo);
            return Ok(new { mensaje = "Base de datos reiniciada." });
        }
    }
}