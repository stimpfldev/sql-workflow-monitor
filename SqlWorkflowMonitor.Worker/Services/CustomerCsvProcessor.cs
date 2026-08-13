using System.Net.Mail;
using SqlWorkflowMonitor.Worker.Models;

namespace SqlWorkflowMonitor.Worker.Services;

public sealed class CustomerCsvProcessor
{
    public async Task<List<StagingCustomer>> ReadAndValidateAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo CSV.",
                filePath);
        }

        var customers = new List<StagingCustomer>();

        using var reader = new StreamReader(filePath);

        // Lee el encabezado.
        string? header =
            await reader.ReadLineAsync(cancellationToken);

        if (header is null)
        {
            throw new InvalidDataException(
                "El archivo CSV está vacío.");
        }

        string normalizedHeader =
            header.Replace(" ", string.Empty);

        if (!normalizedHeader.Equals(
                "Name,Email",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "El encabezado debe ser Name,Email.");
        }

        int lineNumber = 1;

        while (true)
        {
            string? line =
                await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                break;
            }

            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Separa Name y Email.
            string[] columns = line.Split(',', 2);

            string? name = columns.Length >= 1
                ? Normalize(columns[0])
                : null;

            string? email = columns.Length >= 2
                ? Normalize(columns[1])
                : null;

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Name es obligatorio");
            }
            else if (name.Length > 150)
            {
                errors.Add("Name supera los 150 caracteres");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("Email es obligatorio");
            }
            else if (email.Length > 200)
            {
                errors.Add("Email supera los 200 caracteres");
            }
            else if (!MailAddress.TryCreate(email, out _))
            {
                errors.Add("Email tiene un formato inválido");
            }

            customers.Add(new StagingCustomer
            {
                Name = name,
                Email = email,
                IsValid = errors.Count == 0,

                ValidationError = errors.Count == 0
                    ? null
                    : $"Línea {lineNumber}: " +
                      string.Join("; ", errors)
            });
        }

        if (customers.Count == 0)
        {
            throw new InvalidDataException(
                "El archivo CSV no contiene registros.");
        }

        return customers;
    }

    private static string? Normalize(string value)
    {
        string normalized = value.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}