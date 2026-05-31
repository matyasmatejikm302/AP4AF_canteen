# UTB.Minute - Canteen Management System

Tento projekt je komplexním řešením pro správu vysokoškolské jídelny (menzy) vytvořeným v rámci předmětu **Aplikované frameworky (AF / AP4AF)** na Fakultě aplikované informatiky Univerzity Tomáše Bati ve Zlíně.

## Autoři projektu

* **Radek Čechman**
* **Matyáš Matějík**

**Poměr práce:** 1:1 (Autoři se na vývoji, návrhu architektury, datového modelu, integraci a testování podíleli rovným dílem).

---

## Splnění nefunkčních požadavků vyučujícího

Níže je uveden přehled, jak projekt splňuje striktní nefunkční požadavky ze zadání vyučujícího:

| Požadavek ze zadání | Způsob implementace v projektu |
| :--- | :--- |
| **Platforma .NET 10** | Celé řešení je postaveno na nejnovější verzi **.NET 10** a C# 14. |
| **Anglický jazyk v kódu** | Veškeré zdrojové kódy (názvy tříd, vlastností, metod, databázových entit, DTOs i xUnit testů) jsou striktně v **anglickém jazyce**. Uživatelské rozhraní a testovací data (seed) jsou v češtině. |
| **.NET Aspire orchestrace** | Projekt plně využívá .NET Aspire pro Service Discovery, orchestraci kontejnerů (PostgreSQL, Keycloak) a správu telemetrických dat. |
| **Entity Framework Core** | Datová vrstva (`UTB.Minute.Db`) využívá EF Core s PostgreSQL poskytovatelem. Jsou implementovány migrace pro vytváření a správu schématu. |
| **Minimal Web API & DTOs** | WebAPI (`UTB.Minute.WebApi`) implementuje čisté REST endpointy s využitím `TypedResults`. Datové přenosové objekty (DTOs) jsou zcela nezávislé na EF entitách a jsou sdíleny v assembly `UTB.Minute.Contracts`. WebAPI nikdy nevrací databázové entity přímo. |
| **Server-Sent Events (SSE)** | SSE je implementováno ve WebAPI pro okamžitou distribuci změn stavů objednávek. Je anonymní a dostupné bez autorizace. Na straně klienta je ošetřeno streamování odpovědí v prohlížeči (Fetch API). |
| **BFF a proxy komunikace** | Klientské aplikace nevolají WebAPI napřímo. Komunikace probíhá přes zabezpečený serverový proxy (BFF - Backend-for-Frontend) v projektu `UTB.Minute.Web`, který mimo jiné řeší i zabezpečení relací a předávání JWT tokenů z Keycloaku. |
| **xUnit testy s reálnou DB** | Integrační testy v `UTB.Minute.WebApi.Tests` nevyužívají InMemory EF Core poskytovatele. Pomocí `DistributedApplicationTestingBuilder` ze sady .NET Aspire automaticky spouštějí reálné izolované PostgreSQL kontejnery, nad kterými testy autonomně běží. |

---

## Architektura a struktura řešení

Řešení je rozděleno do následujících logických projektů v solution:

1. **`UTB.Minute.AppHost`** – Hlavní orchestrátor Aspire, který spouští a propojuje PostgreSQL, Keycloak, WebAPI, klientský server a pomocné nástroje.
2. **`UTB.Minute.ServiceDefaults`** – Sdílené nastavení pro OpenTelemetry, metriky, logování a vyhodnocování stavu služeb (Health Checks).
3. **`UTB.Minute.Db`** – Databázová vrstva s EF Core kontextem (`AppDbContext`), entitami (`Meal`, `MenuItem`, `Order`) a migracemi pro PostgreSQL.
4. **`UTB.Minute.Contracts`** – Sdílená knihovna obsahující datové přenosové objekty (DTOs) a společné enumy (např. `OrderStateDto`).
5. **`UTB.Minute.WebApi`** – Minimal Web API poskytující služby pro správu jídel, menu a objednávek a vysílající SSE události.
6. **`UTB.Minute.DbManager`** – Servisní API určené pro vývojový cyklus. Poskytuje Http Command pro bezpečný reset a seedování databáze.
7. **`UTB.Minute.WebApi.Tests`** – Integrační xUnit testy pro ověření byznys logiky API nad skutečnou kontejnerizovanou databází.
8. **`UTB.Minute.Web`** – Serverová část klientské aplikace (BFF), která hostuje Blazor WebAssembly, spravuje OpenID Connect (OIDC) relaci s Keycloakem a funguje jako reverzní proxy pro API.
9. **`UTB.Minute.Web.Client`** – Klientská Blazor WebAssembly aplikace, která se spouští v prohlížeči uživatele a obsahuje klientské stránky a komponenty.

---

## Uživatelské role a autentizace (Keycloak)

Zabezpečení aplikace je realizováno pomocí identity provideru **Keycloak** (OIDC):
* **Student (veřejná role):** Nepřihlašuje se. Má volný přístup na stránku jídelního lístku (`/canteen`), kde vidí dnešní nabídku, může objednávat jídlo pod anonymním identifikátorem a v reálném čase sledovat stav své objednávky.
* **Kuchařka (zabezpečená role):** Vyžaduje přihlášení přes Keycloak. Po úspěšném ověření a zjištění role `canteen-cook` je uživatel automaticky přesměrován do rozhraní kuchyně (`/kitchen`), kde spravuje přípravu a výdej objednávek.
* **Vedení menzy (zabezpečená role):** Vyžaduje přihlášení přes Keycloak do administrativního rozhraní pro tvorbu jídel a sestavování menu.

Úvodní stránka `/` slouží jako **inteligentní rozcestník (router)**. Pokud uživatel není přihlášen, zobrazí se čisté přihlašovací rozhraní. Po přihlášení aplikace na základě JWT tokenu a rolí automaticky přesměruje kuchařku do kuchyně a studenta do jídelny.

---

## Jak projekt spustit a otestovat

### Prerekvizity
* Nainstalované **.NET 10 SDK** (Preview).
* Spuštěný **Docker Desktop** (nutný pro běh PostgreSQL a Keycloak kontejnerů).

### Krok 1: Spuštění aplikace
1. Otevřete solution ve Visual Studiu.
2. Nastavte projekt **`UTB.Minute.AppHost`** jako spouštěcí (Startup Project).
3. Spusťte aplikaci (F5).
4. V prohlížeči se otevře **.NET Aspire Dashboard**.

### Krok 2: Inicializace a seedování databáze
Při prvním spuštění (nebo po restartu kontejneru) je PostgreSQL databáze prázdná. Pro naplnění testovacími daty:
1. V Aspire Dashboardu najděte řádek se službou **`dbmanager`**.
2. V pravé části (sloupec *Actions / Commands*) klikněte na tlačítko **"Restart Database"**.
3. Tím se spustí Http Command `/dev/seed`, který bezpečně smaže případné staré tabulky, aplikuje nejnovější migrace a naplní databázi testovacím menu (Svíčková, Řízek, Čočka na kyselo atd.).

### Krok 3: Otevření klientského rozhraní
V Aspire Dashboardu klikněte na odkaz u služby **`web`** (Blazor BFF Server). 
* **Důležité:** Vždy používejte zabezpečený protokol **HTTPS** (např. **`https://localhost:7011/`**). 
* Při použití HTTPS prohlížeč Chrome správně zpracuje zabezpečené cookies (`SameSite` a `Secure` atributy) předávané mezi Keycloakem a klientskou aplikací, což zamezí chybám typu HTTP 431 a zablokovanému přihlašování.

### Krok 4: Testování reálného času (SSE)
Chcete-li otestovat okamžitou synchronizaci objednávek bez obnovování stránky:
1. Otevřete běžné okno prohlížeče na adrese studenta: `https://localhost:7011/canteen`. Uvidíte stav **SSE: Aktivní** (zeleně).
2. Otevřete **anonymní okno** (Ctrl+Shift+N) na adrese `https://localhost:7011/`.
3. Klikněte na **Přihlásit se přes Keycloak** a přihlaste se jako kuchařka (např. pomocí Keycloak účtu s rolí `canteen-cook` nebo lokálním demo přihlášením). Budete přesměrováni na `/kitchen`.
4. V okně studenta klikněte u jídla na **OBJEDNAT**.
5. V okně kuchařky se v tutéž sekundu automaticky zobrazí nová objednávka. Kuchařka může kliknout na **Hotovo k vydání** a následně **Vydat jídlo** – student v prvním okně uvidí všechny změny stavu v reálném čase.

### Krok 5: Spuštění automatizovaných integračních testů
Testy můžete spustit přímo v IDE přes **Průzkumník testů (Test Explorer)** nebo pomocí CLI příkazu v adresáři projektu `UTB.Minute.WebApi.Tests`:
```bash
dotnet test