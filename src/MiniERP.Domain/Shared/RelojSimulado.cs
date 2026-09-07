using System.Globalization;

/// <summary>
/// Reloj de la simulacion. SOLO existe en la rama sim/reloj-virtual: main no lo tiene.
/// <para>
/// Si la variable de entorno MINIERP_RELOJ apunta a un archivo, la hora de negocio se lee
/// de ese archivo; si no existe la variable, se comporta EXACTAMENTE como DateTime.UtcNow.
/// Sin namespace a proposito: asi lo ven las cuatro capas y todos los .razor sin anadir un using.
/// </para>
/// </summary>
public static class RelojSimulado
{
    private static readonly string? Ruta = Environment.GetEnvironmentVariable("MINIERP_RELOJ");
    private static readonly object Candado = new();
    private static DateTime _valor;
    private static long _ticksUltimaLectura;

    /// <summary>Hora UTC de negocio: la del archivo del reloj, o la real si no hay simulacion.</summary>
    public static DateTime UtcNow
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Ruta))
                return DateTime.UtcNow;

            var real = DateTime.UtcNow;
            lock (Candado)
            {
                // Relectura con cache corto: el orquestador reescribe el archivo antes de cada
                // operacion y entre una y otra pasan segundos, asi que 100 ms sobran.
                if (real.Ticks - _ticksUltimaLectura > TimeSpan.TicksPerMillisecond * 100)
                {
                    try
                    {
                        var texto = File.ReadAllText(Ruta).Trim();
                        if (DateTime.TryParse(texto, CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                                out var leido))
                        {
                            _valor = DateTime.SpecifyKind(leido, DateTimeKind.Utc);
                        }
                    }
                    catch
                    {
                        // Archivo a medio escribir: se conserva el ultimo valor bueno.
                    }

                    _ticksUltimaLectura = real.Ticks;
                }

                return _valor == default ? real : _valor;
            }
        }
    }

    /// <summary>Equivalente simulado de DateTime.Now.</summary>
    public static DateTime Now => UtcNow;

    /// <summary>Equivalente simulado de DateTime.Today.</summary>
    public static DateTime Today => UtcNow.Date;
}
