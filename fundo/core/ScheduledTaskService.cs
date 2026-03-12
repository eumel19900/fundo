using System;
using System.Collections.Generic;
using System.Text;

namespace fundo.core
{
    /*
     ich schreibe eine anwendung in c# winui3. ich deploye als msix über den windows store.
Meine Anwendung soll sich selbst in windows als scheduled task eintragen.
Wie findet meine anwendung dafür ihren eigenen executable pfad für den eintrag in scheduled tasks heraus?

Bei einer WinUI 3 App, die als MSIX über den Microsoft Store installiert wird, ist das etwas anders als bei klassischen EXE-Apps.

Der Grund:
MSIX-Apps werden nicht in einem festen, stabilen Pfad installiert, sondern in einem versionsabhängigen Ordner wie z. B.:

C:\Program Files\WindowsApps\<PackageName>_<Version>_x64__<PublisherId>\


Dieser Pfad:

kann sich bei jedem Update ändern

ist geschützt

sollte nicht hart codiert werden

Deshalb musst du ihn zur Laufzeit ermitteln.

✅ Richtiger Weg in .NET / WinUI3

Den Pfad der aktuell laufenden EXE bekommst du mit:

using System.Diagnostics;

string exePath = Process.GetCurrentProcess().MainModule.FileName;


oder moderner:

string exePath = Environment.ProcessPath!;


Das liefert z. B.:

C:\Program Files\WindowsApps\MyApp_1.0.3.0_x64__abc123\MyApp.exe


Diesen Pfad kannst du direkt für den Scheduled Task verwenden.

⚠️ Wichtig bei MSIX Apps

Ein Problem bleibt:
Wenn deine App aktualisiert wird, ändert sich der Installationsordner (Versionsnummer).

Der Scheduled Task würde dann noch auf:

...MyApp_1.0.3.0...


zeigen, obwohl installiert ist:

...MyApp_1.0.4.0...


→ Task startet nicht mehr.

🟢 Besserer Ansatz für MSIX Apps

Statt direkt die EXE zu starten, solltest du die App über ihre AppUserModelId starten.

MSIX Apps werden intern so gestartet:

explorer.exe shell:AppsFolder\<AppUserModelId>


Die AppUserModelId bekommst du so:

using Windows.ApplicationModel;

string appId = Package.Current.Id.FamilyName + "!App";


Dann kann dein Scheduled Task z. B. ausführen:

explorer.exe shell:AppsFolder\<AppUserModelId>


Das funktioniert auch nach Updates.

⚠️ Noch ein wichtiger Punkt

Apps aus dem Microsoft Store dürfen oft keine Scheduled Tasks erstellen, außer:

sie nutzen FullTrustProcess

oder spezielle Background Tasks aus der Windows App SDK.

Microsoft empfiehlt normalerweise:

Windows Background Task

App Services

statt klassische Scheduled Tasks.

💡 Mein Praxis-Tipp für WinUI3

Die stabilste Lösung ist oft:

kleiner Helper-EXE außerhalb von MSIX

Helper.exe
   → Scheduled Task
   → startet WinUI App


Damit vermeidest du:

Versionspfad-Probleme

Store-Restriktionen

✅ Wenn du willst, kann ich dir auch zeigen:

die 3 Zeilen Code, mit denen man einen Scheduled Task aus C# erstellt

wie Store-Apps trotzdem Autostart bekommen (ohne Scheduled Task – das ist oft die bessere Lösung).
     * */





    internal class ScheduledTaskService
    {
        public static void EnsureScheduledTaskIsSetup()
        {
            if (Settings.AutomaticIndexUpdateEnabled)
            {
                // Schedule the automatic index update task based on user settings
                // This is a placeholder for the actual scheduling logic, which would depend on the platform (e.g., Windows Task Scheduler, a background service, etc.)
                Console.WriteLine("Scheduling automatic index update task...");
            }
            else
            {
                // If automatic updates are disabled, ensure any existing scheduled tasks are removed
                Console.WriteLine("Automatic index update is disabled. Removing any existing scheduled tasks...");
            }
        }
    }
}
