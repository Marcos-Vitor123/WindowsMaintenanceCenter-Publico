# Windows Maintenance Center (WMC)

<p align="center">
  <strong>Ferramenta completa de manutenção, limpeza e otimização para Windows</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/C%23-Latest-239120?logo=csharp" alt="C#">
  <img src="https://img.shields.io/badge/WPF-UI-9B4DCA" alt="WPF">
  <img src="https://img.shields.io/badge/Licença-MIT-green" alt="MIT License">
  <img src="https://img.shields.io/badge/Windows-10%2F11-0078D4?logo=windows" alt="Windows 10/11">
</p>

---

## Sobre o Projeto

O **Windows Maintenance Center (WMC)** é uma aplicação desktop desenvolvida em **C#/.NET 10** com interface em **WPF**, projetada para ser uma solução completa e moderna para manutenção, limpeza e otimização de sistemas Windows.

O projeto é uma **reescrita completa** de um aplicativo original desenvolvido em Python/Tkinter, trazendo benefícios significativos como arquitetura modular (MVVM + DI), interface moderna estilo Windows 11, persistência de dados, automação de tarefas e diagnóstico avançado via WMI.

### Propósito

O WMC foi criado para fornecer aos usuários Windows uma ferramenta centralizada e acessível para:

- **Limpar arquivos temporários e desnecessários** que ocupam espaço em disco
- **Reparar integridade do sistema** usando DISM e SFC
- **Realizar limpeza profunda** com ResetBase do DISM
- **Diagnosticar o estado do sistema** com informações detalhadas via WMI
- **Gerenciar programas de inicialização** diretamente pelo registro do Windows
- **Agendar manutenções automáticas** sem intervenção do usuário

---

## Screenshots

<p align="center">
  <img src="docs/img/01-inicio.png" width="700" alt="Tela Inicial">
</p>

<p align="center">
  <img src="docs/img/02-manuntencao.png" width="700" alt="Tela Manutenção">
</p>

<p align="center">
  <img src="docs/img/03-diagnóstico.png" width="700" alt="Tela Diagnóstico">
</p>

<p align="center">
  <img src="docs/img/04-inicializacao.png" width="700" alt="Tela Inicialização">
</p>

<p align="center">
  <img src="docs/img/05-configuracoes.png" width="700" alt="Tela Configurações">
</p>

<p align="center">
  <img src="docs/img/06-historico.png" width="700" alt="Tela Histórico">
</p>

---

## Funcionalidades

### Interface (6 Abas)

| Aba | Descrição |
|-----|-----------|
| **Inicio** | Dashboard com status do sistema, última otimização, espaço liberado total e botão rápido de otimização diária |
| **Manutenção** | 6 cartões de ação com tarefas de manutenção de不同的 níveis |
| **Diagnóstico** | Informações detalhadas do sistema: versão Windows, RAM, disco, recomendações inteligentes |
| **Inicialização** | Gerenciador de programas que iniciam com o Windows (ativação/desativação via Registro) |
| **Configurações** | Configurações gerais, modo de operação (Assistido/Automático) e frequência de tarefas |
| **Histórico** | Log completo de todas as operações com data, duração, resultado e espaço liberado |

### Tarefas de Manutenção

| Tarefa | Descrição | Comandos Utilizados |
|--------|-----------|---------------------|
| **Otimização Diária** | Limpeza rápida de arquivos temporários | `del /q /f /s %TEMP%\*` + `cleanmgr /sagerun:1` |
| **Reparação do Sistema** | Verificação e reparo de integridade | DISM CheckHealth, ScanHealth, RestoreHealth + `sfc /scannow` |
| **Limpeza Leve** | Limpeza de sistema + componentes | `cleanmgr /sageset:1` + DISM StartComponentCleanup |
| **Limpeza Profunda** | Limpeza máxima com ResetBase | `cleanmgr /sagerun:1` + DISM StartComponentCleanup `/ResetBase` |
| **Reparação + Limpeza Leve** | Reparo seguido de limpeza | Pipeline de reparo + Limpeza Leve |
| **Reparação Completa** | Manutenção total do sistema | Reparo + CHKDSK + Limpeza Profunda |

### Arquitetura Back-end

O projeto conta com **10 serviços** especializados que executam as operações de sistema:

- **MaintenanceEngine** - Executor genérico de comandos de manutenção via `cmd.exe`
- **SystemRepairEngine** - Pipeline completo de reparo (DISM + SFC + CHKDSK)
- **DeepCleanEngine** - Motor de limpeza profunda com cleanmgr e DISM ResetBase
- **DiagnosticService** - Coleta de informações do sistema via WMI (Win32_OperatingSystem, Win32_ComputerSystem)
- **StartupManager** - Gerenciamento de programas de inicialização via Registro (HKLM/HKCU Run)
- **AutomationService** - Timer para execução automática de tarefas agendadas
- **ConfigService** - Persistência de configurações em JSON (`Config/configuracoes.json`)
- **HistoryService** - Persistência de histórico de operações em JSON (`Logs/historico.json`)
- **NotificationService** - Sistema de notificações para o usuário
- **SoundService** - Feedback sonoro via SystemSounds do Windows

### Sons do Sistema

| Evento | Som |
|--------|-----|
| Operação concluída | `SystemSounds.Asterisk` |
| Aviso/Sucesso parcial | `SystemSounds.Exclamation` |
| Erro | `SystemSounds.Hand` |

---

## Requisitos

| Requisito | Detalhe |
|-----------|---------|
| **Sistema Operacional** | Windows 10 ou Windows 11 |
| **Runtime** | [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **Privilégios** | Executar como **Administrador** (obrigatório para DISM, SFC, CHKDSK) |
| **Disco** | ~50 MB de espaço livre para instalação |

---

## Instalação e Build

### Compilar a partir do código fonte

```bash
# Clonar o repositório
git clone https://github.com/Marcos-Vitor123/WindowsMaintenanceCenter-Publico.git
cd WindowsMaintenanceCenter-Publico

# Compilar
dotnet build

# Executar (necessário ser administrador)
dotnet run
```

### Publicação auto-contida

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

---

## Estrutura do Projeto

```
WindowsMaintenanceCenter/
├── App.xaml / App.xaml.cs              # Ponto de entrada, container DI
├── MainWindow.xaml / .cs               # (Não utilizado - scaffold leftover)
├── Converters.cs                       # Value converters para data binding
├── docs/img/                           # Screenshots do programa
│
├── Models/                             # Modelos de dados
│   ├── AppConfig.cs                    # Configurações do usuário
│   ├── HistoryEntry.cs                 # Entrada de log de operação
│   ├── MaintenanceTask.cs             # Definição de tarefa
│   ├── StartupEntry.cs                # Programa de inicialização
│   └── SystemInfo.cs                  # Informações do sistema
│
├── Services/                           # Lógica de negócio
│   ├── ILogger.cs                      # Interface de log
│   ├── MaintenanceEngine.cs            # Executor de comandos
│   ├── SystemRepairEngine.cs           # Reparo DISM/SFC/CHKDSK
│   ├── DeepCleanEngine.cs              # Limpeza profunda
│   ├── DiagnosticService.cs            # Diagnóstico WMI
│   ├── StartupManager.cs              # Gerenciador de inicialização
│   ├── AutomationService.cs            # Automação por timer
│   ├── ConfigService.cs               # Persistência de config
│   ├── HistoryService.cs              # Persistência de histórico
│   ├── NotificationService.cs          # Notificações
│   └── SoundService.cs                # Sons do sistema
│
├── ViewModels/                         # ViewModels MVVM
│   ├── ViewModelBase.cs               # Base INotifyPropertyChanged
│   ├── MainViewModel.cs               # Shell e navegação
│   ├── HomeViewModel.cs               # Dashboard
│   ├── MaintenanceViewModel.cs         # Tarefas de manutenção
│   ├── DiagnosticsViewModel.cs         # Diagnóstico
│   ├── StartupViewModel.cs            # Programas de inicialização
│   ├── SettingsViewModel.cs           # Configurações
│   └── HistoryViewModel.cs            # Histórico
│
├── Views/                              # Interface WPF
│   ├── MainWindow.xaml / .cs           # Janela principal (shell)
│   ├── HomeView.xaml / .cs             # Página inicial
│   ├── MaintenanceView.xaml / .cs      # Página de manutenção
│   ├── DiagnosticsView.xaml / .cs      # Página de diagnóstico
│   ├── StartupView.xaml / .cs          # Página de inicialização
│   ├── SettingsView.xaml / .cs         # Página de configurações
│   ├── HistoryView.xaml / .cs          # Página de histórico
│   └── Converters.cs                  # Converters da Views
│
├── Resources/
│   └── Styles.xaml                     # Estilos WPF compartilhados
│
├── Config/
│   └── configuracoes.json              # Configurações persistidas (runtime)
│
└── Logs/
    ├── historico.json                  # Histórico de operações (runtime)
    └── log_YYYYMMDD.txt               # Logs diários (runtime)
```

---

## Diferenças em Relação ao Projeto Original (Python/Tkinter)

| Aspecto | Python/Tkinter | WMC (.NET/WPF) |
|---------|----------------|-----------------|
| **Arquitetura** | Monolítica | MVVM + Dependency Injection |
| **Interface** | Tkinter básica | WPF moderno estilo Windows 11 |
| **Navegação** | 5 botões | 6 abas com sidebar |
| **Persistência** | Nenhuma | JSON (config + histórico) |
| **Gerenciador de inicialização** | Não | Sim (Registro Windows) |
| **Modo automático** | Não | Sim (agendamento por tarefa) |
| **Diagnóstico** | Básico | WMI (Win32_OperatingSystem, Win32_ComputerSystem) |
| **Sons** | Sim | Sim (SystemSounds) |
| **Reparo de sistema** | Parcial | Pipeline completo (DISM + SFC + CHKDSK) |

---

## Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## Contribuição

Contribuições são bem-vindas! Para contribuir:

1. Faça um Fork do repositório
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Faça commit das suas alterações (`git commit -m 'Adiciona nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Abra um Pull Request

---

## Autor

**Marcos Vitor** - [GitHub](https://github.com/Marcos-Vitor123)

---

## Desenvolvimento

Este projeto foi desenvolvido com **Inteligência Artificial (IA)** em conjunto com **interação humana**, onde cada decisão, comando e resultado passou por **revisão e validação do desenvolvedor**. A IA atuou como ferramenta de auxílio na geração de código, documentação e arquitetura, sendo o humano o responsável pela supervisão, correção e aprovação final de todo o conteúdo.
