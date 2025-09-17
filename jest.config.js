module.exports = {
    projects: [
        {
            displayName: "root",
            testEnvironment: "node",
            testMatch: ["<rootDir>/test/**/*.test.js"],
            testPathIgnorePatterns: ["/node_modules/", "/auth-service/"], // removed '/platform-host/'
        },
        "<rootDir>/platform-host/platform-host-frontend/jest.config.js",
        "<rootDir>/modules/cms-module/cms-frontend/jest.config.mjs",
    ],
};
