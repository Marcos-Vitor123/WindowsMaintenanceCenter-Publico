# Arquitetura do Windows Maintenance Center

Documento técnico detalhando a arquitetura, padrões de design e estrutura do projeto WMC.

---

## 1. Visão Geral da Arquitetura

O WMC utiliza o padrão **MVVM (Model-View-ViewModel)** com **Dependency Injection (DI)**, aplicado sobre a framework **WPF (.NET 10)**. Esta escolha foi motivada pela necessidade de:

- Separação clara entre lógica de negócio e apresentação
- Testabilidade dos componentes
- Manutenção e escalabilidade do código
- Integração nativa com data binding do WPF

### Diagrama de Camadas

```
┌──────────────────────────────────────────────────────────────────┐
│                        APRESENTAÇÃO (WPF)                        │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────────────────┐ │
│  │   Views/     │  │  Resources/ │  │      Converters/         │ │
│  │  (XAML +     │  │ Styles.xaml │  │  (Value Converters)      │ │
│  │  Code-behind)│  │             │  │                          │ │
│  └──────┬──────┘  └─────────────┘  └──────────────────────────┘ │
│         │ Data Binding                                          │
├─────────┼────────────────────────────────────────────────────────┤
│         ▼                    MVVM                                │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                      ViewModels/                             │ │
│  │  MainViewModel ──┬── HomeViewModel                          │ │
│  │                  ├── MaintenanceViewModel                    │ │
│  │                  ├── DiagnosticsViewModel                    │ │
│  │                  ├── StartupViewModel                        │ │
│  │                  ├── SettingsViewModel                       │ │
│  │                  └── HistoryViewModel                        │ │
│  └─────────────────────────────┬───────────────────────────────┘ │
│                                │                                 │
│                                │ Injeção de Dependência          │
├────────────────────────────────┼─────────────────────────────────┤
│                                ▼           NEGÓCIO               │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                       Services/                              │ │
│  │  ┌──────────────────┐  ┌────────────────────────────────┐  │ │
│  │  │ MaintenanceEngine │  │ SystemRepairEngine             │  │ │
│  │  │ DeepCleanEngine   │  │ DiagnosticService              │  │ │
│  │  │ StartupManager    │  │ AutomationService              │  │ │
│  │  │ ConfigService     │  │ HistoryService                 │  │ │
│  │  │ NotificationSvc   │  │ SoundService                   │  │ │
│  │  └──────────────────┘  └────────────────────────────────┘  │ │
│  └─────────────────────────────┬───────────────────────────────┘ │
│                                │                                 │
│                                │ Execução                        │
├────────────────────────────────┼─────────────────────────────────┤
│                                ▼          SISTEMA                │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │  cmd.exe /c  │  WMI (System.Management)  │  Registry API    │ │
│  │  DISM, SFC,  │  Win32_OperatingSystem    │  HKLM/HKCU Run  │ │
│  │  CHKDSK,     │  Win32_ComputerSystem     │                  │ │
│  │  cleanmgr    │  DriveInfo                │                  │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                    PERSISTÊNCIA                              │ │
│  │  Config/configuracoes.json  │  Logs/historico.json          │ │
│  │  Logs/log_YYYYMMDD.txt                                       │ │
│  └─────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

---

## 2. Padrões de Design Utilizados

### 2.1 MVVM (Model-View-ViewModel)

A separação MVVM é implementada da seguinte forma:

| Camada | Responsabilidade | Componentes |
|--------|------------------|-------------|
| **Model** | Estruturas de dados e DTOs | `AppConfig`, `HistoryEntry`, `MaintenanceTask`, `StartupEntry`, `SystemInfo` |
| **View** | Apresentação visual (XAML) | `HomeView`, `MaintenanceView`, `DiagnosticsView`, `StartupView`, `SettingsView`, `HistoryView` |
| **ViewModel** | Lógica de apresentação e comandos | `MainViewModel`, `HomeViewModel`, `MaintenanceViewModel`, etc. |

**Convenções implementadas:**
- Todos os ViewModels herdam de `ViewModelBase` (implementa `INotifyPropertyChanged`)
- Comandos usam `RelayCommand` (implementação genérica com `Action<T>` e `Func<T, bool>`)
- Data binding unidirecional: View → ViewModel → Service
- Conversores de valor (`IValueConverter`) para transformar dados no XAML

### 2.2 Dependency Injection (DI)

O container de DI é configurado em `App.xaml.cs`:

```csharp
// Registro como Singleton (estado compartilhado)
services.AddSingleton<ConfigService>();
services.AddSingleton<HistoryService>();
services.AddSingleton<NotificationService>();
services.AddSingleton<SoundService>();
services.AddSingleton<AutomationService>();

// Registro como Transient (nova instância por resolved)
services.AddTransient<MaintenanceEngine>();
services.AddTransient<SystemRepairEngine>();
services.AddTransient<DeepCleanEngine>();
services.AddTransient<DiagnosticService>();
services.AddTransient<StartupManager>();
```

### 2.3 Command Pattern

Todos os comandos de UI são implementados via `RelayCommand`:

```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    
    // Executa a ação vinculada ao botão
    public void Execute(object? parameter) => _execute(parameter);
    
    // Verifica se o comando pode ser executado
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
}
```

### 2.4 Observer Pattern

O `NotificationService` implementa o padrão Observer para notificações:

```csharp
// Evento que Views podem assinar
public event EventHandler<NotificationEventArgs>? NotificationReceived;

// Disparar notificação
NotificationService.Notify("Operação concluída!", NotificationType.Success);
```

### 2.5 Strategy Pattern (implícito)

Os engines de manutenção implementam diferentes estratégias de execução:
- `MaintenanceEngine` → Execução genérica de comandos
- `SystemRepairEngine` → Pipeline de reparo (DISM → SFC → CHKDSK)
- `DeepCleanEngine` → Pipeline de limpeza profunda

---

## 3. Ciclo de Vida e Fluxo de Dados

### 3.1 Fluxo de Inicialização

```
App.OnStartup()
  │
  ├── 1. ServiceCollection build
  │     ├── Registra todos os Services (Singleton)
  │     ├── Registra todos os ViewModels (Transient)
  │     └── Registra todas as Views (Transient)
  │
  ├── 2. ConfigService.Load()
  │     └── Lê Config/configuracoes.json
  │
  ├── 3. AutomationService.Start()
  │     └── Inicia timer (1min inicial → 1h repetição)
  │
  └── 4. MainWindow.Show()
        ├── Verifica privilégios de Admin
        └── Inicia com HomeView (dashboard)
```

### 3.2 Fluxo de Execução de uma Tarefa

```
Usuário clica no botão
  │
  ▼
View (XAML) → Command binding
  │
  ▼
ViewModel.ExecuteTask(taskId)
  │
  ├── 1. Exibe confirmação (MessageBox)
  │
  ├── 2. MaintenanceEngine.Execute(task)
  │     ├── Monta comando (cmd.exe /c ...)
  │     ├── Inicia processo redirecionando stdout/stderr
  │     ├── Captura código de saída
  │     ├── Estima espaço liberado
  │     └── Retorna resultado
  │
  ├── 3. HistoryService.Record(entry)
  │     └── Serializa para Logs/historico.json
  │
  ├── 4. NotificationService.Notify(message)
  │     └── Dispara evento para UI
  │
  └── 5. SoundService.Play(sound)
        └── Toca SystemSounds apropriado
```

### 3.3 Fluxo de Automação

```
AutomationService (timer a cada 1h)
  │
  ├── 1. ConfigService.GetTaskSchedules()
  │     └── Lê frequência configurada para cada tarefa
  │
  ├── 2. HistoryService.GetLastExecution(taskId)
  │     └── Verifica última execução no histórico
  │
  ├── 3. Calcula se está atrasado
  │     └── (hoje - última_execução) > frequência_configurada
  │
  ├── 4. Se atrasado E modo automático:
  │     ├── MaintenanceEngine.Execute(task)
  │     ├── HistoryService.Record(entry)
  │     └── NotificationService.Notify(...)
  │
  └── 5. Agenda próxima verificação (1h)
```

---

## 4. Componentes Detalhados

### 4.1 Models (5 classes)

| Modelo | Arquivo | Descrição |
|--------|---------|-----------|
| `AppConfig` | `Models/AppConfig.cs` | Configurações do usuário: StartWithWindows, AutoMode, TaskSchedules |
| `HistoryEntry` | `Models/HistoryEntry.cs` | Registro de operação: data, função, duração, sucesso, espaço liberado |
| `MaintenanceTask` | `Models/MaintenanceTask.cs` | Definição de tarefa: id, nome, comando, requerRestart, isDeepClean |
| `StartupEntry` | `Models/StartupEntry.cs` | Programa de inicialização: nome, publisher, impacto, habilitado, caminho registro |
| `SystemInfo` | `Models/SystemInfo.cs` | Info do sistema: versão Windows, RAM, disco, status |

### 4.2 Services (11 componentes)

| Serviço | Tipo | Responsabilidade |
|---------|------|------------------|
| `ILogger` | Interface | Contrato para serviços de log |
| `MaintenanceEngine` | Engine | Executa comandos via `cmd.exe /c`, redireciona stdout/stderr |
| `SystemRepairEngine` | Engine | Pipeline: DISM CheckHealth → ScanHealth → RestoreHealth → SFC ScanNow → CHKDSK |
| `DeepCleanEngine` | Engine | cleanmgr /sageset:1 + cleanmgr /sagerun:1 + DISM StartComponentCleanup /ResetBase |
| `DiagnosticService` | Service | Coleta info via WMI (Win32_OperatingSystem, Win32_ComputerSystem) + DriveInfo |
| `StartupManager` | Service | Lê/escreve Registry HKLM/HKCU Run; desativa renomeando com prefixo `_disabled_` |
| `AutomationService` | Service | Timer periódico que verifica e executa tarefas atrasadas (modo automático) |
| `ConfigService` | Service + ILogger | Persiste config em JSON; implementa logging para arquivos diários |
| `HistoryService` | Service + ILogger | Persiste histórico em JSON (max 1000 entradas); thread-safe com locks |
| `NotificationService` | Service | Event-based dispatcher para notificações de UI |
| `SoundService` | Service | Toca SystemSounds: Asterisk, Exclamation, Hand |

### 4.3 ViewModels (8 classes)

| ViewModel | Página | Responsabilidade Principal |
|-----------|--------|---------------------------|
| `ViewModelBase` | - | Base abstrata com `INotifyPropertyChanged` e `SetProperty<T>()` |
| `MainViewModel` | Shell | Navegação entre páginas, orquestração dos serviços |
| `HomeViewModel` | Inicio | Dashboard: status, recomendações, ação rápida |
| `MaintenanceViewModel` | Manutenção | 6 cartões de tarefa, execução com confirmação |
| `DiagnosticsViewModel` | Diagnóstico | Info do sistema, recomendações inteligentes |
| `StartupViewModel` | Inicialização | Lista de programas, toggle enable/disable |
| `SettingsViewModel` | Configurações | Configurações gerais, modo, agendamento, reset |
| `HistoryViewModel` | Histórico | Tabela de operações, refresh, clear |

### 4.4 Views (7 UserControls)

| View | Arquivo XAML | Bindings Principais |
|------|-------------|---------------------|
| `MainWindow` | `Views/MainWindow.xaml` | Sidebar navigation, ContentControl → CurrentViewModel |
| `HomeView` | `Views/HomeView.xaml` | SystemStatus, LastOptimization, TotalSpaceFreed, Recommendations |
| `MaintenanceView` | `Views/MaintenanceView.xaml` | Tasks collection, ExecuteTaskCommand |
| `DiagnosticsView` | `Views/DiagnosticsView.xaml` | SystemInfo properties, Recommendations |
| `StartupView` | `Views/StartupView.xaml` | StartupEntries, ToggleCommand |
| `SettingsView` | `Views/SettingsView.xaml` | AppConfig properties, SaveCommand |
| `HistoryView` | `Views/HistoryView.xaml` | HistoryEntries, RefreshCommand, ClearCommand |

---

## 5. Persistência de Dados

### 5.1 Configurações

- **Arquivo:** `Config/configuracoes.json`
- **Formato:** JSON serializado via `System.Text.Json`
- **Conteúdo:** Objeto `AppConfig` com todas as preferências do usuário
- **Ciclo:** Carregado na inicialização, salvo a cada alteração

```json
{
  "startWithWindows": false,
  "minimizeToTray": true,
  "autoMode": false,
  "taskSchedules": {
    "TempFiles": 1,
    "Prefetch": 30,
    "RecycleBin": 3,
    "DiskCleanup": 7,
    "SystemRepair": 30
  }
}
```

### 5.2 Histórico de Operações

- **Arquivo:** `Logs/historico.json`
- **Formato:** Array JSON de objetos `HistoryEntry`
- **Limite:** Máximo 1000 entradas (as mais antigas são removidas)
- **Thread-safe:** Utiliza `lock` para acesso concorrente

### 5.3 Logs Diários

- **Arquivo:** `Logs/log_YYYYMMDD.txt`
- **Formato:** Texto append-only com timestamps
- **Conteúdo:** Mensagens de log dos serviços que implementam `ILogger`

---

## 6. Segurança e Permissões

### 6.1 Verificação de Administrador

O WMC requer privilégios de administrador para operar corretamente. A verificação é feita na inicialização:

```csharp
// Views/MainWindow.xaml.cs
private void Window_SourceInitialized(object? sender, EventArgs e)
{
    var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
    {
        MessageBox.Show("Execute como Administrador!", "Aviso");
    }
}
```

### 6.2 Operações que Requerem Admin

| Operação | Por quê |
|----------|---------|
| DISM (CheckHealth, ScanHealth, RestoreHealth) | Modifica componentes do Windows |
| SFC /scannow | Repara arquivos do sistema |
| CHKDSK | Acessa estrutura baixa do disco |
| cleanmgr | Acessa pastas protegidas |
| Registry HKLM | Modifica configurações globais |

### 6.3 Mecanismo de Desativação de Startup

O `StartupManager` desativa programas **sem deletar** a entrada do registro:

```csharp
// Renomeia: "MeuApp" → "_disabled_MeuApp"
string disabledName = "_disabled_" + originalValueName;
registryKey.SetValue(disabledName, originalValue);
registryKey.DeleteValue(originalValueName, false);
```

---

## 7. Tecnologias e Dependências

### 7.1 Stack Tecnológica

| Camada | Tecnologia | Versão |
|--------|------------|--------|
| **Runtime** | .NET | 10.0 |
| **Linguagem** | C# | Latest (file-scoped namespaces, nullable) |
| **UI Framework** | WPF | .NET 10 |
| **DI Container** | Microsoft.Extensions.DependencyInjection | 9.0.0 |
| **WPF Behaviors** | Microsoft.Xaml.Behaviors.Wpf | 1.1.142 |
| **System Management** | System.Management (WMI) | 10.0.10 |
| **Serialização** | System.Text.Json | Built-in |
| **Registry API** | Microsoft.Win32.Registry | Built-in |

### 7.2 Dependências NuGet

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
  <PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.142" />
  <PackageReference Include="System.Management" Version="10.0.10" />
</ItemGroup>
```

---

## 8. Padrões de Código

### 8.1 Convenções de Nomenclatura

| Elemento | Convenção | Exemplo |
|----------|-----------|---------|
| Classes | PascalCase | `MaintenanceEngine` |
| Propriedades | PascalCase | `SystemStatus` |
| Métodos | PascalCase | `ExecuteTask()` |
| Campos privados | _camelCase | `_notificationService` |
| Variáveis locais | camelCase | `lastEntry` |
| Constantes | PascalCase | `MaxHistoryEntries` |
| Enums | PascalCase | `PageType.Home` |

### 8.2 Nullable Reference Types

O projeto habilita `nullable` no `.csproj`:

```xml
<Nullable>enable</Nullable>
```

Isto garante que referências potencialmente nulas sejam tratadas estaticamente pelo compilador.

### 8.3 Async/Await

Operações de I/O (leitura de arquivos, WMI) utilizam async/await:

```csharp
// Exemplo: DiagnosticService
public async Task<SystemInfo> GetSystemInfoAsync()
{
    // WMI queries são async
    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
    // ...
}
```

---

## 9. Estrutura de Build e Publicação

### 9.1 Configuração do Projeto

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### 9.2 Comandos de Build

```bash
# Debug
dotnet build

# Release
dotnet build -c Release

# Publicação auto-contida (sem .NET Runtime necessário)
dotnet publish -c Release -r win-x64 --self-contained

# Publicação dependente do Runtime
dotnet publish -c Release -r win-x64 --no-self-contained
```

---

## 10. Roadmap e Melhorias Futuras

| Prioridade | Melhoria | Descrição |
|------------|----------|-----------|
| Alta | Unit Tests | Criar projeto de testes com xUnit ou NUnit |
| Alta | Consolidar Converters | Remover duplicação entre `Converters.cs` raiz e `Views/Converters.cs` |
| Alta | Remover MainWindow duplicado | Deletar `MainWindow.xaml` raiz (scaffold leftover) |
| Média | Medição real de espaço | Calcular espaço antes/depois em vez de estimativas hardcoded |
| Média | Re-enabling de Startup | Armazenar valor original ao desativar para reativação correta |
| Média | Logging estruturado | Migrar para Serilog ou extensions logging |
| Baixa | Internacionalização | Suporte a múltiplos idiomas (PT/EN) |
| Baixa | Tema escuro | Implementar tema dark mode |
| Baixa | Auto-update | Verificação automática de atualizações |

---

## 11. Diagrama de Componentes (Resumo)

```
┌─────────────────────────────────────────────────────────────────┐
│                     Windows Maintenance Center                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐   │
│  │  HOME   │    │MANUTENÇÃO│    │DIAGNOST. │    │INICIALIZ.│   │
│  └────┬────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘   │
│       │              │               │               │          │
│       └──────────────┴───────┬───────┴───────────────┘          │
│                              │                                   │
│                    ┌─────────▼─────────┐                        │
│                    │   MainViewModel    │                        │
│                    │  (Navegação/Shell) │                        │
│                    └─────────┬─────────┘                        │
│                              │                                   │
│              ┌───────────────┼───────────────┐                  │
│              │               │               │                  │
│  ┌───────────▼──┐  ┌────────▼─────┐  ┌──────▼──────────┐      │
│  │  Services    │  │  Persistency │  │  Automation      │      │
│  │  - MaintEng  │  │  - Config    │  │  - Timer         │      │
│  │  - RepairEng │  │  - History   │  │  - Scheduling    │      │
│  │  - Diagnos   │  │  - Logs      │  │  - Auto-exec     │      │
│  │  - Startup   │  │              │  │                  │      │
│  └──────────────┘  └──────────────┘  └──────────────────┘      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 12. Nota sobre o Desenvolvimento

Este projeto foi desenvolvido com **Inteligência Artificial (IA)** em conjunto com **interação humana**, onde cada decisão, comando e resultado passou por **revisão e validação do desenvolvedor**. A IA atuou como ferramenta de auxílio na geração de código, documentação e arquitetura, sendo o humano o responsável pela supervisão, correção e aprovação final de todo o conteúdo.
