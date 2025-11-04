#!/usr/bin/env dotnet-script
// ====================================================================
// TestEmail.csx : Script de test pour l'envoi d'emails SMTP
// ====================================================================
// Ce script permet de tester la configuration SMTP de l'application
// en envoyant un email de test.
//
// UTILISATION :
// dotnet script Scripts/TestEmail.csx
//
// PRÉREQUIS :
// - Installer dotnet-script : dotnet tool install -g dotnet-script
// - Configurer les paramètres SMTP dans appsettings.json
//
// PARAMÈTRES :
// Modifiez les variables ci-dessous avant d'exécuter le script.

#r "nuget: MailKit, 4.3.0"
#r "nuget: Microsoft.Extensions.Configuration, 8.0.0"
#r "nuget: Microsoft.Extensions.Configuration.Json, 8.0.0"

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.IO;

// ====================================================================
// CONFIGURATION
// ====================================================================

// Email destinataire pour le test
var emailDestinataire = "votre-email@example.com"; // ⚠️ MODIFIEZ CETTE ADRESSE

// Sujet et corps du message
var sujet = "Test email CTSAR Booking";
var corpsMessage = @"
<h1>Email de test</h1>
<p>Ceci est un email de test envoyé depuis l'application CTSAR Booking.</p>
<p>Si vous recevez ce message, votre configuration SMTP fonctionne correctement ! ✅</p>
<hr>
<p style='font-size: 0.9em; color: #666;'>
    Envoyé le : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"
</p>
";

// ====================================================================
// CHARGEMENT DE LA CONFIGURATION
// ====================================================================

Console.WriteLine("🔧 Chargement de la configuration SMTP...\n");

// Chemin vers appsettings.json (depuis la racine du projet)
var basePath = Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory));
if (basePath == null)
{
    basePath = Directory.GetCurrentDirectory();
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddUserSecrets("e15d95e1-e70a-4f06-9b11-ef36d57eb98f", optional: true) // ID du projet
    .Build();

// Lecture des paramètres SMTP
var smtpHost = configuration["SmtpSettings:Host"];
var smtpPort = int.Parse(configuration["SmtpSettings:Port"] ?? "587");
var smtpEnableSsl = bool.Parse(configuration["SmtpSettings:EnableSsl"] ?? "true");
var smtpUsername = configuration["SmtpSettings:Username"];
var smtpPassword = configuration["SmtpSettings:Password"];
var smtpFromEmail = configuration["SmtpSettings:FromEmail"];
var smtpFromName = configuration["SmtpSettings:FromName"];

// Affichage de la configuration (sans le mot de passe)
Console.WriteLine("📧 Configuration SMTP :");
Console.WriteLine($"   Host        : {smtpHost}");
Console.WriteLine($"   Port        : {smtpPort}");
Console.WriteLine($"   SSL/TLS     : {smtpEnableSsl}");
Console.WriteLine($"   Username    : {smtpUsername}");
Console.WriteLine($"   Password    : {new string('*', smtpPassword?.Length ?? 0)}");
Console.WriteLine($"   From Email  : {smtpFromEmail}");
Console.WriteLine($"   From Name   : {smtpFromName}");
Console.WriteLine();

// Validation des paramètres
if (string.IsNullOrEmpty(smtpHost) ||
    string.IsNullOrEmpty(smtpUsername) ||
    string.IsNullOrEmpty(smtpPassword))
{
    Console.WriteLine("❌ ERREUR : Paramètres SMTP manquants !");
    Console.WriteLine();
    Console.WriteLine("Configurez les paramètres via User Secrets :");
    Console.WriteLine("  dotnet user-secrets set \"SmtpSettings:Host\" \"smtp.gmail.com\"");
    Console.WriteLine("  dotnet user-secrets set \"SmtpSettings:Port\" \"587\"");
    Console.WriteLine("  dotnet user-secrets set \"SmtpSettings:EnableSsl\" \"true\"");
    Console.WriteLine("  dotnet user-secrets set \"SmtpSettings:Username\" \"votre-email@gmail.com\"");
    Console.WriteLine("  dotnet user-secrets set \"SmtpSettings:Password\" \"votre-mot-de-passe-app\"");
    Console.WriteLine("  dotnet user-secrets set \"SmtpSettings:FromEmail\" \"noreply@ctsar.fr\"");
    Console.WriteLine("  dotnet user-secrets set \"SmtpSettings:FromName\" \"CTSAR Booking\"");
    return;
}

if (emailDestinataire == "votre-email@example.com")
{
    Console.WriteLine("❌ ERREUR : Veuillez modifier la variable 'emailDestinataire' dans le script !");
    Console.WriteLine($"   Ouvrez le fichier : {Path.Combine("Scripts", "TestEmail.csx")}");
    Console.WriteLine($"   Ligne 25 : var emailDestinataire = \"votre-email@example.com\";");
    return;
}

// ====================================================================
// CRÉATION DU MESSAGE
// ====================================================================

Console.WriteLine($"✉️  Création du message pour {emailDestinataire}...\n");

var message = new MimeMessage();
message.From.Add(new MailboxAddress(smtpFromName, smtpFromEmail));
message.To.Add(MailboxAddress.Parse(emailDestinataire));
message.Subject = sujet;

var bodyBuilder = new BodyBuilder();
bodyBuilder.HtmlBody = corpsMessage;
message.Body = bodyBuilder.ToMessageBody();

// ====================================================================
// ENVOI DE L'EMAIL
// ====================================================================

try
{
    Console.WriteLine("📤 Connexion au serveur SMTP...");

    using var client = new SmtpClient();

    // Connexion
    await client.ConnectAsync(
        smtpHost,
        smtpPort,
        smtpEnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

    Console.WriteLine("✅ Connecté au serveur SMTP");

    // Authentification
    Console.WriteLine("🔐 Authentification...");
    await client.AuthenticateAsync(smtpUsername, smtpPassword);
    Console.WriteLine("✅ Authentification réussie");

    // Envoi
    Console.WriteLine($"📨 Envoi du message à {emailDestinataire}...");
    await client.SendAsync(message);
    Console.WriteLine("✅ Message envoyé avec succès !");

    // Déconnexion
    await client.DisconnectAsync(true);

    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine("✅ TEST RÉUSSI !");
    Console.WriteLine("========================================");
    Console.WriteLine($"Email envoyé à : {emailDestinataire}");
    Console.WriteLine($"Sujet          : {sujet}");
    Console.WriteLine($"Date           : {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine("❌ ERREUR LORS DE L'ENVOI");
    Console.WriteLine("========================================");
    Console.WriteLine($"Type    : {ex.GetType().Name}");
    Console.WriteLine($"Message : {ex.Message}");
    Console.WriteLine();

    if (ex.InnerException != null)
    {
        Console.WriteLine("Détails supplémentaires :");
        Console.WriteLine($"  {ex.InnerException.Message}");
        Console.WriteLine();
    }

    Console.WriteLine("Vérifications à faire :");
    Console.WriteLine("  1. Les paramètres SMTP sont corrects");
    Console.WriteLine("  2. Le mot de passe est un 'App Password' pour Gmail");
    Console.WriteLine("  3. Le port 587 est accessible (pare-feu)");
    Console.WriteLine("  4. SSL/TLS est activé");
    Console.WriteLine();
}
