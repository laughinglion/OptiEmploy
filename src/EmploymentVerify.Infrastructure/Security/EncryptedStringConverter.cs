using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EmploymentVerify.Infrastructure.Security;

public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IFieldEncryption encryption)
        : base(
            v => v == null ? null : encryption.Encrypt(v),
            v => v == null ? null : encryption.Decrypt(v))
    {
    }
}
