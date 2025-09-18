import React from "react";

interface HealthStatus {
    status: string;
    timestamp: string;
    version: string;
    service: string;
    module: string;
}

const HealthCheck: React.FC = () => {
    const healthData: HealthStatus = {
        status: "healthy",
        timestamp: new Date().toISOString(),
        version: "1.0.0",
        service: "CMS Frontend",
        module: "cmsModule",
    };

    return (
        <div
            style={{
                padding: "20px",
                fontFamily: "monospace",
                backgroundColor: "#f0f0f0",
                border: "1px solid #ccc",
                borderRadius: "4px",
                margin: "20px",
            }}
        >
            <h2>CMS Module Health Check</h2>
            <pre>{JSON.stringify(healthData, null, 2)}</pre>
            <div style={{ marginTop: "20px", fontSize: "12px", color: "#666" }}>
                <p>Module Federation Entry: ./CmsApp, ./CmsRouter</p>
                <p>Development Server: https://cms-fe.platform.local:3003</p>
                <p>GrapesJS: {typeof window !== "undefined" && (window as any).grapesjs ? "Loaded" : "Loading..."}</p>
            </div>
        </div>
    );
};

export default HealthCheck;
