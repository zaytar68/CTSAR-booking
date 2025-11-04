#!/usr/bin/env dotnet-script
#r "nuget: MailKit, 4.14.1"
#r "nuget: System.Text.Json, 8.0.0"

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

// ====================================================================
// Script de test d'envoi d'email SMTP
// ====================================================================
// Ce script lit les paramètres SMTP depuis appsettings.json et envoie
// un email de test à une adresse hardcodée.
//
// Utilisation :
//   dotnet script TestEmail.csx
// ====================================================================

const string RECIPIENT_EMAIL = "cedric.tirolf@gmail.com";

Console.WriteLine("=== Test d'envoi d'email SMTP ===\n");

try
{
    // Lire appsettings.json
    var appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

    if (!File.Exists(appsettingsPath))
    {
        Console.WriteLine($"❌ ERREUR: Le fichier appsettings.json n'existe pas à : {appsettingsPath}");
        return 1;
    }

    Console.WriteLine($"📄 Lecture de la configuration depuis : {appsettingsPath}");

    var jsonContent = File.ReadAllText(appsettingsPath);
    var options = new JsonDocumentOptions
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    var jsonDoc = JsonDocument.Parse(jsonContent, options);
    var smtpSettings = jsonDoc.RootElement.GetProperty("SmtpSettings");

    var host = smtpSettings.GetProperty("Host").GetString();
    var port = smtpSettings.GetProperty("Port").GetInt32();
    var enableSsl = smtpSettings.GetProperty("EnableSsl").GetBoolean();
    var username = smtpSettings.GetProperty("Username").GetString();
    var password = smtpSettings.GetProperty("Password").GetString();
    var fromEmail = smtpSettings.GetProperty("FromEmail").GetString();
    var fromName = smtpSettings.GetProperty("FromName").GetString();

    Console.WriteLine($"\n📧 Configuration SMTP :");
    Console.WriteLine($"   Serveur : {host}:{port}");
    Console.WriteLine($"   SSL     : {enableSsl}");
    Console.WriteLine($"   De      : {fromName} <{fromEmail}>");
    Console.WriteLine($"   Vers    : {RECIPIENT_EMAIL}");
    Console.WriteLine();

    // Créer le message
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress(fromName, fromEmail));
    message.To.Add(new MailboxAddress(RECIPIENT_EMAIL, RECIPIENT_EMAIL));
    message.Subject = "Test SMTP - CTSAR Booking";

    var bodyBuilder = new BodyBuilder
    {
        HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #1976d2;'>✅ Test d'envoi SMTP réussi</h2>
        <p>Ceci est un email de test envoyé depuis <strong>CTSAR Booking</strong>.</p>
        <p>Si vous recevez ce message, cela signifie que la configuration SMTP fonctionne correctement.</p>
        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
        <p style='font-size: 12px; color: #666;'>
            Date d'envoi : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"
        </p>
    </div>
</body>
</html>"
    };

    message.Body = bodyBuilder.ToMessageBody();

    Console.WriteLine("📤 Envoi de l'email en cours...");

    // Envoyer l'email
    using (var client = new SmtpClient())
    {
        // Port 587 nécessite STARTTLS, port 465 nécessite SSL/TLS
        if (port == 587)
        {
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            Console.WriteLine($"✓ Connexion établie avec {host}:{port} (Mode: STARTTLS)");
        }
        else if (enableSsl)
        {
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.SslOnConnect);
            Console.WriteLine($"✓ Connexion établie avec {host}:{port} (Mode: SSL/TLS)");
        }
        else
        {
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.None);
            Console.WriteLine($"✓ Connexion établie avec {host}:{port} (Mode: Non sécurisé)");
        }

        await client.AuthenticateAsync(username, password);
        Console.WriteLine($"✓ Authentification réussie pour {username}");

        await client.SendAsync(message);
        Console.WriteLine($"✓ Email envoyé à {RECIPIENT_EMAIL}");

        await client.DisconnectAsync(true);
        Console.WriteLine($"✓ Déconnexion du serveur SMTP");
    }

    Console.WriteLine($"\n✅ SUCCESS: Email de test envoyé avec succès !");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERREUR lors de l'envoi de l'email :");
    Console.WriteLine($"   Type    : {ex.GetType().Name}");
    Console.WriteLine($"   Message : {ex.Message}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Détails : {ex.InnerException.Message}");
    }

    Console.WriteLine($"\n📋 Stack trace :");
    Console.WriteLine(ex.StackTrace);

    return 1;
}
