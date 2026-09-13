# Smart SQL Studio

Un'applicazione desktop moderna per la gestione di database SQL Server, creata con .NET 8 e Avalonia UI.

## 🚀 Caratteristiche Principali

### IntelliSense Avanzato
- **Completamento intelligente** delle parole chiave SQL
- **Suggerimenti contestuali** basati sul testo digitato
- **Snippet integrati** per le operazioni più comuni
- **Priorità intelligente** dei suggerimenti (keywords > tabelle > snippet)

### Snippet Preconfigurati
- SELECT base e con JOIN
- INSERT, UPDATE, DELETE
- CREATE TABLE
- Stored Procedure template
- CTE (Common Table Expressions)
- Funzioni Window (ROW_NUMBER, RANK, etc.)
- Gestione errori TRY/CATCH
- E molti altri...

### Interfaccia Moderna
- **Tema scuro** ispirato a Visual Studio Code
- **Editor di codice** AvaloniaEdit con evidenziazione sintassi SQL
- **Pannello snippet** ricercabile e organizzato per categorie
- **Pannello risultati** con informazioni su tempi di esecuzione

### Connessione Database
- Supporto autenticazione Windows e SQL Server
- Generazione automatica connection string
- Test connessione in tempo reale
- Esecuzione query asincrona

## 📁 Struttura del Progetto

```
SmartSqlStudio/
├── Models/           # Modelli dati (Snippet, Connection, QueryResult)
├── ViewModels/       # ViewModel con MVVM pattern
├── Views/            # Interfacce utente AXAML
├── Services/         # Servizi (SnippetService, IntelliSenseService, DatabaseService)
├── App.axaml         # Configurazione applicazione
└── Program.cs        # Punto di ingresso
```

## 🔧 Requisiti

- .NET 8.0 SDK
- Un IDE compatibile con Avalonia (Visual Studio 2022, Rider, VS Code)

## 🛠️ Build e Esecuzione

```bash
# Ripristina i pacchetti
dotnet restore

# Compila il progetto
dotnet build

# Esegui l'applicazione
dotnet run
```

## 💡 Ottimizzazioni Implementate

1. **IntelliSense Contestuale**: Analizza il testo corrente e fornisce suggerimenti pertinenti
2. **Ricerca Fuzzy negli Snippet**: Trova snippet per nome, descrizione, contenuto o keyword
3. **Esecuzione Asincrona**: Le query non bloccano l'interfaccia utente
4. **Cache degli Snippet**: Caricamento iniziale rapido con snippet predefiniti
5. **Priorità dei Suggerimenti**: Keywords SQL hanno priorità massima

## 🎯 Prossime Migliorie (Roadmap)

- [ ] Integrazione con schema database reale per completamento tabelle/colonne
- [ ] Grid dati completo per visualizzazione risultati
- [ ] Salvataggio snippet personalizzati su file/DB
- [ ] Storia delle query eseguite
- [ ] Esportazione risultati (CSV, Excel, JSON)
- [ ] Multi-tab per query multiple
- [ ] Evidenziazione errori di sintassi in tempo reale
- [ ] Formattazione automatica del codice SQL
- [ ] Supporto multi-piattaforma avanzato (Linux, macOS)

## 📄 License

MIT License - Libero utilizzo per scopi personali e commerciali.

---

**Nota**: .NET 10 non è ancora rilasciato (al momento della creazione). Questo progetto utilizza .NET 8.0 LTS che è stabile e supportato. Quando .NET 10 sarà disponibile, basterà aggiornare il `<TargetFramework>` nel file `.csproj`.
