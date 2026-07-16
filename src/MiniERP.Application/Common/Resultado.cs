namespace MiniERP.Application.Common;

/// <summary>
/// Desenlace de una operacion de negocio.
/// </summary>
/// <remarks>
/// Que un codigo este repetido o que falte existencia no es un fallo del programa:
/// es una respuesta esperada que la pantalla debe mostrar. Devolverlo como valor y no
/// como excepcion mantiene el flujo legible y deja las excepciones para lo imprevisto.
/// </remarks>
public class Resultado
{
    protected Resultado(bool exito, string? error)
    {
        Exito = exito;
        Error = error;
    }

    public bool Exito { get; }

    public string? Error { get; }

    public bool Fallo => !Exito;

    public static Resultado Ok() => new(true, null);

    public static Resultado Falla(string error) => new(false, error);

    public static Resultado<T> Ok<T>(T valor) => new(valor, true, null);

    public static Resultado<T> Falla<T>(string error) => new(default, false, error);
}

public class Resultado<T> : Resultado
{
    internal Resultado(T? valor, bool exito, string? error) : base(exito, error) => Valor = valor;

    public T? Valor { get; }
}
