# Role of Test Personas

## Different Test Personas

Different people, ranging from "code-first" testers to members of the maker community, will play distinct roles in the testing process:
-	Code-First Testers: Developers with a deep understanding of testing methodologies and tools.
-	Maker Community: Business users with limited technical knowledge who can leverage low-code/no-code testing tools.
-   Reviewers: Who want to perform gated release and approval of changes and test results cso that they can be reviewed prior to release
-   Support Engineers: WHo want to Execute tests that validate functionality and operational health of deployed solution. They may also author new tests to illustrate an issue that needs to be resolved.

## Scaling the Impact of testing

Scalability is achieved by providing appropriate tools and training for each persona, enabling them to contribute effectively to the testing process. Depending on the team and the technology landscape they could look to mix and match different elements to best support their quality strategy

| Key Elements	| Role | Notes |
|---------------|-------------------|--------------------|
| Tooling | Code-First Testers| Local editors (e.g., Visual Studio, Visual Studio Code) |
| Tooling | Maker Community | Browser-based record and authoring tools |
| Execution	| Code-First Testers | Integrated into CI/CD tooling. For example Azure DevOps or GitHub	|
| Execution	| Maker Community |  Low Code deployment pipelines
| Operational Tests	| Maker Community | Heath Check tests. For example Cloud flow triggered test
