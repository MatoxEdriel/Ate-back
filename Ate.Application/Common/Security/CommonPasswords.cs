namespace Ate.Application.Common.Security;

/// <summary>
/// Contraseñas de 12+ caracteres muy usadas (listas públicas de filtraciones y variantes en español).
/// Se comparan sin distinguir mayúsculas. Es un filtro mínimo, no reemplaza a una lista completa.
/// </summary>
internal static class CommonPasswords
{
    private static readonly HashSet<string> Values = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012", "1234567890123", "12345678901234", "123456123456", "111111111111", "000000000000",
        "123123123123", "121212121212", "112233445566", "987654321098", "098765432109", "123456654321",
        "qwertyuiopas", "qwertyuiop12", "qwerty123456", "qwertyqwerty", "asdfghjklñ12", "asdfghjkl123",
        "zxcvbnm12345", "1q2w3e4r5t6y", "1qaz2wsx3edc", "qazwsxedcrfv", "q1w2e3r4t5y6", "abcdefghijkl",
        "abc123abc123", "abcd12345678", "a1b2c3d4e5f6", "password1234", "password12345", "passwordpassword",
        "password123!", "Password1234", "Password123!", "P@ssw0rd1234", "p@ssword1234", "iloveyou1234",
        "letmein12345", "welcome12345", "welcome123!!", "admin1234567", "administrator", "administrador",
        "admin123admin", "adminadmin12", "superadmin123", "changeme1234", "default12345", "secret123456",
        "contraseña123", "contraseña1234", "contrasena123", "contrasena1234", "micontraseña1", "micontrasena1",
        "contraseña12345", "Contraseña123", "Contraseña1234", "clave12345678", "miclave12345", "clavesegura1",
        "teamo1234567", "teamomucho12", "123456789abc", "1234567890ab", "123456789qwe", "123qweasdzxc",
        "qweasdzxc123", "dragon123456", "monkey123456", "football1234", "baseball1234", "sunshine1234",
        "princess1234", "superman1234", "batman123456", "starwars1234", "pokemon12345", "minecraft123",
        "Ecuador12345", "ecuador12345", "ecuador2024!", "ecuador2025!", "ecuador2026!", "guayaquil123",
        "guayaquil1234", "quito1234567", "barcelona123", "barcelonasc1", "emelec123456", "liga12345678",
        "Ate12345678!", "ate123456789", "ateapp123456", "comida123456", "almuerzo1234", "bienvenido12",
        "bienvenido123", "Bienvenido123", "bienvenida123", "holahola1234", "holamundo123", "12345678ab!",
        "Admin123*Ate", "Admin1234567", "Temporal1234", "temporal1234", "Cambiar12345", "cambiar12345",
    };

    public static bool Contains(string password) => Values.Contains(password);
}
