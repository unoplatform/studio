# Uno Platform Studio Repository

Welcome to the [**Uno Platform Studio**](https://aka.platform.uno/studio) Repository! 

**Uno Platform Studio** is an AI-native productivity platform for building enterprise-grade, cross-platform .NET applications. It brings together purpose-built tools—from prompt-to-app generation and an AI development agent, to design handoff and the industry-first, cross-platform runtime visual designer for .NET, Hot Design<sup>®</sup>—so developers can design, build, and iterate on apps for every platform .NET runs on, all while staying in their flow.

## New in Uno Platform Studio 3.0: AI-native development for enterprise .NET

AI coding tools can generate code quickly, but for enterprises, generating code is not the same as building a real application. Production apps need design system awareness, live runtime context, cross-platform consistency, UI validation, and a human reviewer in the loop. Generic AI agents struggle to:

- See the live visual tree of the running application.
- Know the design system, theme, and component library the team has standardized on.
- Validate that a change renders correctly across web, desktop, mobile, and embedded targets.
- Pull context from official, version-correct Uno Platform documentation.
- Stay inside an approval workflow that a human controls.

**Uno Platform Studio 3.0** closes that gap by giving AI agents the context and validation loops they need to build real apps, through two new components that work alongside Hot Design<sup>®</sup>, Hot Reload, Design-to-Code, the Uno MCP, and the App MCP:

- **Uno Platform Studio App** — a browser-based way to go from a prompt to a working Uno Platform app in minutes, with no installs required. Start from a description—or one of the curated samples in the Gallery—watch the app build live thanks to Hot Reload, refine it through the conversation panel, then export to your preferred IDE or CLI to keep building. [Get started](xref:Uno.PlatformStudio.GetStarted)

- **Uno Platform Studio Agent** — the orchestration layer that connects AI models to the Uno Platform development workflow. It contributes deep, cross-platform .NET domain knowledge, manages context, scaffolds apps from best practices, grounds responses in the latest Uno Platform documentation, and invokes MCP tools to verify its own work. It ships with 70+ Uno Platform–specific skills—spanning themes, Material Design, MVUX state management, navigation, toolkit controls, and UI test automation—delivered through the single `uno-platform-studio` plugin for Claude Code, GitHub Copilot, and OpenAI Codex, all inside a human-in-the-loop workflow. [Learn more](xref:Uno.PlatformStudio.Skills)

## Productivity tools

**Uno Platform Studio** revolutionizes how developers design, build, and iterate on their applications by leveraging purpose-built tools that streamline your workflow:

- **[Uno Platform Studio App](ref:Uno.PlatformStudio.GetStarted)**
  Describe what you want and generate a working, well-structured Uno Platform app in the browser, then continue building in your IDE.

- **[Hot Design<sup>®</sup>](https://aka.platform.uno/hot-design)**
  The industry-first, runtime visual designer, for cross-platform .NET applications. Hot Design<sup>®</sup> transforms your running app into a Designer, from any IDE, on any OS, to create polished interfaces with ease. Pair it with the **[Hot Design<sup>®</sup> Agent](xref:Uno.HotDesign.Agent)** for AI-powered UX/UI creation that leverages your data contexts and live previews. [Get started](xref:Uno.HotDesign.GetStarted.Guide)

- **[Hot Reload](https://aka.platform.uno/hot-reload)**
  Reliably update any code in your app and get instant confirmation your changes were applied, with a Hot Reload Indicator to monitor changes while you develop.

- **[Design-to-Code](https://aka.platform.uno/Design-to-Code)**
  Generate ready-to-use, well-structured XAML or C# Markup directly from your Figma designs with one click, completely eliminating manual design handoff. [Get started](xref:Uno.Figma.GetStarted)

- **[Uno MCP & App MCP](https://aka.platform.uno/using-uno-mcps)**
  The **Uno MCP** provides structured, semantic access to Uno Platform's complete knowledge base—covering documentation, APIs, and best practices—empowering AI agents and developers with the intelligence they need to build better experiences. The **App MCP** brings intelligent automation to life by enabling AI agents to interact directly with live Uno Platform applications, creating a seamless bridge between design, development, and execution.

## We want to hear from you!

If you're using Uno Platform Studio, we encourage you to share your thoughts or report any issues you encounter here.

You can [open issues](https://github.com/unoplatform/studio/issues) for the following purposes:

- **Bug Report**: Encountered a bug while using Uno Platform Studio? Let us know so we can address it.
- **Documentation Issue**: Found an issue in the documentation? Report it here.
- **Documentation Request**: Have ideas to improve or enhance our documentation? Let us know.
- **Enhancement Request**: Propose new features or enhancements for Uno Platform Studio.
- **Feedback**: Share your thoughts and feedback with us ❤️.
- **Questions**: For general inquiries about Uno Platform Studio, visit the [Discussions](https://github.com/unoplatform/studio/discussions) tab.
- **Security Vulnerabilities**: If you discover a security vulnerability, please report it through the [security policy](SECURITY.md).

For additional support, visit our [Discord Server](https://platform.uno/uno-discord) – where our engineering team and community will be happy to assist you.

For organizations seeking a deeper level of support beyond our community support, please [contact us](https://platform.uno/contact).

We value your input and appreciate your contributions to improving Uno Platform Studio!
