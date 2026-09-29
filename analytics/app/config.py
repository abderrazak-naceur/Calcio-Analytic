"""Application configuration.

Settings are loaded from environment variables (and an optional .env file)
using pydantic-settings.
"""

from __future__ import annotations

from typing import Optional

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Environment-driven application settings."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # Port the analytics service listens on.
    analytics_port: int = 8000

    # Optional database connection string (e.g. Postgres).
    database_url: Optional[str] = None

    # Deployment environment: "development", "staging", "production", etc.
    app_env: str = "development"

    # Comma-separated list of allowed CORS origins (frontend).
    cors_origins: str = "http://localhost:3000,http://localhost:5173"

    @property
    def cors_origin_list(self) -> list[str]:
        """Return CORS origins as a cleaned list."""
        return [origin.strip() for origin in self.cors_origins.split(",") if origin.strip()]


settings = Settings()
