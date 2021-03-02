# TeamCloud Providers

[TeamCloud](https://github.com/microsoft/TeamCloud) is a tool that enables enterprise IT organizations to provide application development teams "self-serve" access to secure compliant cloud development environments.

![TeamCloud-Providers Build & Packaging](https://github.com/microsoft/TeamCloud-Providers/workflows/Create%20Pre-release/badge.svg)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/microsoft/teamcloud-providers?label=Release%20%28main%29)
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/microsoft/teamcloud-providers?include_prereleases&label=Release%20%28dev%29)

This repository contains TeamCloud Providers.  In the context of TeamCloud, a Provider represents an abstract implementation of a service that manages a resource or resources (i.e. a GitHub repository or an Azure resource) for a cloud development environment (or "Project").

An organization creates and deploys its own Providers or deploys Providers from this repo to Azure.  It then registers the Providers with its TeamCloud instance.  When a development team sends a request to TeamCloud to create a new (or update an existing) Project, TeamCloud invokes each registered Provider to create, update, or delete it's corresponding resource(s).

## About

**TeamCloud and the Providers in this repository are in active development and will change.**  As the these Providers become ready for use, they will be [versioned](https://semver.org/) and released.

We will do our best to conduct all development openly by [documenting](https://github.com/microsoft/TeamCloud-Providers/tree/main/docs) features and requirements, and managing the project using [issues](https://github.com/microsoft/TeamCloud-Providers/issues), [milestones](https://github.com/microsoft/TeamCloud-Providers/milestones), and [projects](https://github.com/microsoft/TeamCloud-Providers/projects).

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
