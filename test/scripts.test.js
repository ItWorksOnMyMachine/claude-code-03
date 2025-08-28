const { describe, it, expect, beforeAll, afterAll } = require("@jest/globals");
const fs = require("fs").promises;
const path = require("path");
const { exec } = require("child_process");
const util = require("util");

const execAsync = util.promisify(exec);

describe("Developer Scripts", () => {
    const scriptsDir = path.join(__dirname, "..", "scripts");

    describe("Script Files", () => {
        it("should have start-all script", async () => {
            const scriptPath = path.join(scriptsDir, "start-all.ps1");
            await expect(fs.access(scriptPath)).resolves.not.toThrow();
        });

        it("should have start-deps script", async () => {
            const scriptPath = path.join(scriptsDir, "start-deps.ps1");
            await expect(fs.access(scriptPath)).resolves.not.toThrow();
        });

        it("should have reset-db script", async () => {
            const scriptPath = path.join(scriptsDir, "reset-db.ps1");
            await expect(fs.access(scriptPath)).resolves.not.toThrow();
        });

        it("should have create-user script", async () => {
            const scriptPath = path.join(scriptsDir, "create-user.ps1");
            await expect(fs.access(scriptPath)).resolves.not.toThrow();
        });

        it("should have health-check script", async () => {
            const scriptPath = path.join(scriptsDir, "health-check.ps1");
            await expect(fs.access(scriptPath)).resolves.not.toThrow();
        });

        it("should have logs script", async () => {
            const scriptPath = path.join(scriptsDir, "logs.ps1");
            await expect(fs.access(scriptPath)).resolves.not.toThrow();
        });
    });

    describe("Script Execution", () => {
        describe("start-all.ps1", () => {
            it("should check for docker-compose.yml", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "start-all.ps1"), "utf8");
                expect(scriptContent).toContain("docker-compose.yml");
            });

            it("should start all services with docker-compose up", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "start-all.ps1"), "utf8");
                expect(scriptContent).toMatch(/docker-compose\s+up/);
            });

            it("should support detached mode flag", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "start-all.ps1"), "utf8");
                expect(scriptContent).toContain("-d");
            });
        });

        describe("start-deps.ps1", () => {
            it("should start only dependency services", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "start-deps.ps1"), "utf8");
                // Check that the script adds postgres-platform, postgres-auth, and redis to dockerArgs
                expect(scriptContent).toContain('$dockerArgs += "postgres-platform"');
                expect(scriptContent).toContain('$dockerArgs += "postgres-auth"');
                expect(scriptContent).toContain('$dockerArgs += "redis"');
                expect(scriptContent).toContain('docker-compose @dockerArgs');
            });

            it("should not start application services", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "start-deps.ps1"), "utf8");
                // Check that application services are not added to dockerArgs
                expect(scriptContent).not.toContain('$dockerArgs += "platform-host-bff"');
                expect(scriptContent).not.toContain('$dockerArgs += "auth-service"');
            });
        });

        describe("reset-db.ps1", () => {
            it("should stop containers before reset", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "reset-db.ps1"), "utf8");
                expect(scriptContent).toMatch(/docker-compose\s+down/);
            });

            it("should remove volumes for clean reset", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "reset-db.ps1"), "utf8");
                expect(scriptContent).toContain("-v");
            });

            it("should restart database services after reset", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "reset-db.ps1"), "utf8");
                expect(scriptContent).toMatch(/docker-compose\s+up.*postgres/i);
            });

            it("should wait for databases to be ready", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "reset-db.ps1"), "utf8");
                expect(scriptContent).toMatch(/wait|sleep|timeout/i);
            });
        });

        describe("create-user.ps1", () => {
            it("should accept email parameter", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "create-user.ps1"), "utf8");
                expect(scriptContent).toContain("param");
                expect(scriptContent).toContain("email");
            });

            it("should accept password parameter", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "create-user.ps1"), "utf8");
                expect(scriptContent).toContain("param");
                expect(scriptContent).toContain("password");
            });

            it("should accept tenant parameter", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "create-user.ps1"), "utf8");
                expect(scriptContent).toContain("param");
                expect(scriptContent).toContain("tenant");
            });

            it("should call development API endpoint", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "create-user.ps1"), "utf8");
                expect(scriptContent).toMatch(/api\/dev\/users|localhost:5000/i);
            });
        });

        describe("health-check.ps1", () => {
            it("should check PostgreSQL platform database", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "health-check.ps1"), "utf8");
                expect(scriptContent).toContain("5432");
                expect(scriptContent).toContain("platform_db");
            });

            it("should check PostgreSQL auth database", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "health-check.ps1"), "utf8");
                expect(scriptContent).toContain("5433");
                expect(scriptContent).toContain("auth_db");
            });

            it("should check Redis service", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "health-check.ps1"), "utf8");
                expect(scriptContent).toContain("6379");
                expect(scriptContent).toContain("redis");
            });

            it("should check BFF service", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "health-check.ps1"), "utf8");
                expect(scriptContent).toMatch(/5000|platform-bff/i);
            });

            it("should check Auth service", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "health-check.ps1"), "utf8");
                expect(scriptContent).toMatch(/5001|auth-service/i);
            });

            it("should provide clear status output", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "health-check.ps1"), "utf8");
                expect(scriptContent).toMatch(/write-host|echo/i);
                expect(scriptContent).toMatch(/status|health|running/i);
            });
        });

        describe("logs.ps1", () => {
            it("should use docker-compose logs command", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "logs.ps1"), "utf8");
                expect(scriptContent).toMatch(/docker-compose\s+logs/);
            });

            it("should support following logs in real-time", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "logs.ps1"), "utf8");
                expect(scriptContent).toContain("-f");
            });

            it("should support filtering by service", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "logs.ps1"), "utf8");
                expect(scriptContent).toContain("param");
                expect(scriptContent).toContain("service");
            });

            it("should support tail parameter for last N lines", async () => {
                const scriptContent = await fs.readFile(path.join(scriptsDir, "logs.ps1"), "utf8");
                expect(scriptContent).toContain("tail");
            });
        });
    });

    describe("Script Documentation", () => {
        const scripts = ["start-all.ps1", "start-deps.ps1", "reset-db.ps1", "create-user.ps1", "health-check.ps1", "logs.ps1"];

        scripts.forEach((script) => {
            describe(`${script} documentation`, () => {
                it("should have a description comment at the top", async () => {
                    const scriptContent = await fs.readFile(path.join(scriptsDir, script), "utf8");
                    expect(scriptContent).toMatch(/^#\s+.+/m);
                });

                it("should document parameters if applicable", async () => {
                    const scriptContent = await fs.readFile(path.join(scriptsDir, script), "utf8");
                    if (scriptContent.includes("param")) {
                        expect(scriptContent).toMatch(/#\s+Parameters:|#\s+param:/i);
                    }
                });

                it("should include usage examples", async () => {
                    const scriptContent = await fs.readFile(path.join(scriptsDir, script), "utf8");
                    expect(scriptContent).toMatch(/#\s+Example:|#\s+Usage:/i);
                });
            });
        });
    });

    describe("Cross-platform Compatibility", () => {
        it("should have bash versions for Linux/Mac", async () => {
            const bashScripts = ["start-all.sh", "start-deps.sh", "reset-db.sh", "create-user.sh", "health-check.sh", "logs.sh"];

            for (const script of bashScripts) {
                const scriptPath = path.join(scriptsDir, script);
                await expect(fs.access(scriptPath)).resolves.not.toThrow();
                const content = await fs.readFile(scriptPath, "utf8");
                expect(content).toMatch(/^#!/);
            }
        });
    });

    describe("Error Handling", () => {
        it("start-all should check if Docker is running", async () => {
            const scriptContent = await fs.readFile(path.join(scriptsDir, "start-all.ps1"), "utf8");
            expect(scriptContent).toMatch(/docker\s+version|docker\s+info/);
        });

        it("reset-db should confirm before destroying data", async () => {
            const scriptContent = await fs.readFile(path.join(scriptsDir, "reset-db.ps1"), "utf8");
            expect(scriptContent).toMatch(/confirm|prompt|read-host/i);
        });

        it("create-user should validate email format", async () => {
            const scriptContent = await fs.readFile(path.join(scriptsDir, "create-user.ps1"), "utf8");
            expect(scriptContent).toMatch(/@|email.*validation/i);
        });

        it("scripts should handle missing docker-compose.yml", async () => {
            const scripts = ["start-all.ps1", "start-deps.ps1", "reset-db.ps1"];
            for (const script of scripts) {
                const scriptContent = await fs.readFile(path.join(scriptsDir, script), "utf8");
                expect(scriptContent).toMatch(/test-path|exists|if.*docker-compose/i);
            }
        });
    });
});
