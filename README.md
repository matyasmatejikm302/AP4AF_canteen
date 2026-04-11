# UTB.Minute - Canteen Management System

Tento projekt byl vytvořen jako půlsemestrální odevzdání v rámci předmětu **Aplikované frameworky (AF)** na Fakultě aplikované informatiky UTB. Jedná se o backendový systém pro správu jídelny, postavený na moderních technologiích platformy .NET.

## Autoři projektu
- **Radek Čechman**
- **Matyáš Matějík**

**Poměr práce:** 1:1 (Autoři se na vývoji podíleli rovným dílem).

---

## Technologický stack
Projekt je postaven na nejnovějších technologiích v rámci ekosystému .NET:
- **.NET 10 (Preview)** – využití moderních prvků C# a Minimal APIs.
- **Entity Framework Core** – přístup k databázi (PostgreSQL).
- **.NET Aspire** – orchestrace služeb, Service Discovery a správa kontejnerů.
- **xUnit** – automatizované testování byznys logiky s využitím In-Memory databáze.

## Architektura projektu
- **UTB.Minute.AppHost**: Hlavní orchestrátor projektu (Aspire), který spravuje životní cyklus databáze a ostatních služeb.
- **UTB.Minute.ServiceDefaults**: Centrální konfigurace telemetrie, logování a Health Checks.
- **UTB.Minute.Db**: Datová vrstva obsahující entity, DbContext a konfiguraci vazeb.
- **UTB.Minute.Contracts**: Datové přenosové objekty (DTOs) a sdílené výčtové typy (Enums).
- **UTB.Minute.WebApi**: Samotné API implementující byznys logiku jídelny (Jídla, Menu, Objednávky) s využitím `TypedResults`.
- **UTB.Minute.DbManager**: Servisní API určené pro vývojový cyklus (Seedování a reset databáze).
- **UTB.Minute.WebApi.Tests**: Sada testů ověřující správnost klíčových funkcí (vytvoření objednávky, odečet porcí, deaktivace jídel).

---

## Jak projekt spustit

### Prerekvizity
- Nainstalované **.NET 10 SDK**.
- Spuštěný **Docker Desktop** (nezbytný pro běh PostgreSQL kontejneru přes Aspire).

### Spuštění aplikace
1. Otevřete řešení ve Visual Studiu.
2. Jako spouštěcí projekt nastavte **UTB.Minute.AppHost**.
3. Spusťte aplikaci (F5).
4. Po startu se v prohlížeči otevře **.NET Aspire Dashboard**.

### Inicializace dat (Seedování)
V Aspire Dashboardu v seznamu prostředků (Resources) najděte službu `dbmanager`.
- V pravém sloupci pod záložkou **Commands** klikněte na tlačítko **"Restart Database"**.
- Tím dojde k vyvolání HTTP příkazu `/dev/seed`, který smaže starou databázi, vytvoří novou strukturu a naplní ji testovacími daty (Svíčková, Řízek atd.).

### Testování API
Hlavní API neběží na úvodní stránce (404 je korektní stav). Pro testování funkčnosti využijte:
- **Soubor `UTB.Minute.WebApi.http`** přímo ve Visual Studiu (vyžaduje správné nastavení portu z dashboardu).
- Adresu `https://localhost:<port>/meals` pro výpis jídel přímo v prohlížeči.

### Spuštění testů
Testy lze spustit přímo ve Visual Studiu přes **Průzkumník testů (Test Explorer)**. Všechny testy jsou navrženy jako autonomní a využívají izolovanou In-Memory databázi.