using Microsoft.AspNetCore.Identity;

namespace MiniERP.Infrastructure.Identity;

/// <summary>
/// Mensajes de error de Identity en espanol.
/// </summary>
/// <remarks>
/// Identity trae los suyos en ingles y no los toma de las anotaciones de la pantalla: por
/// mucho que el formulario diga "Contrasena", al fallar responde "Passwords must have at
/// least one digit". Traducir las etiquetas sin traducir esto deja el registro a medias.
///
/// Solo se sobrescriben los que el usuario puede llegar a ver. Los demas —fallos de
/// concurrencia, roles duplicados— los produce el sistema, no una equivocacion suya.
/// </remarks>
public class MensajesDeIdentity : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(
        nameof(DefaultError), "Ocurrio un error inesperado.");

    public override IdentityError DuplicateEmail(string email) => Error(
        nameof(DuplicateEmail), $"Ya hay una cuenta con el correo {email}.");

    public override IdentityError DuplicateUserName(string userName) => Error(
        nameof(DuplicateUserName), $"Ya hay una cuenta con el usuario {userName}.");

    public override IdentityError InvalidEmail(string? email) => Error(
        nameof(InvalidEmail), $"El correo {email} no tiene un formato valido.");

    public override IdentityError InvalidUserName(string? userName) => Error(
        nameof(InvalidUserName), $"El usuario {userName} tiene caracteres que no se permiten.");

    public override IdentityError PasswordTooShort(int length) => Error(
        nameof(PasswordTooShort), $"La contrasena debe tener al menos {length} caracteres.");

    public override IdentityError PasswordRequiresDigit() => Error(
        nameof(PasswordRequiresDigit), "La contrasena debe llevar al menos un numero.");

    public override IdentityError PasswordRequiresLower() => Error(
        nameof(PasswordRequiresLower), "La contrasena debe llevar al menos una letra minuscula.");

    public override IdentityError PasswordRequiresUpper() => Error(
        nameof(PasswordRequiresUpper), "La contrasena debe llevar al menos una letra mayuscula.");

    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(
        nameof(PasswordRequiresNonAlphanumeric),
        "La contrasena debe llevar al menos un simbolo, como . - _ o #.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(
        nameof(PasswordRequiresUniqueChars),
        $"La contrasena debe usar al menos {uniqueChars} caracteres distintos.");

    public override IdentityError PasswordMismatch() => Error(
        nameof(PasswordMismatch), "La contrasena actual no es correcta.");

    public override IdentityError InvalidToken() => Error(
        nameof(InvalidToken), "El enlace no es valido o ya vencio. Pide uno nuevo.");

    public override IdentityError UserAlreadyHasPassword() => Error(
        nameof(UserAlreadyHasPassword), "Esta cuenta ya tiene contrasena.");

    public override IdentityError RecoveryCodeRedemptionFailed() => Error(
        nameof(RecoveryCodeRedemptionFailed), "Ese codigo de recuperacion no es valido o ya se uso.");

    private static IdentityError Error(string codigo, string descripcion) =>
        new() { Code = codigo, Description = descripcion };
}
