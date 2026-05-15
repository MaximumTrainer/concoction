output "api_url" {
  description = "Public URL of the Cloud Run service."
  value       = google_cloud_run_v2_service.api.uri
}

output "artifact_registry_url" {
  description = "Artifact Registry base URL for pushing images."
  value       = "${var.region}-docker.pkg.dev/${var.project_id}/${google_artifact_registry_repository.app.repository_id}"
}

output "cloud_sql_ip" {
  description = "Cloud SQL public IP (schema-discovery source)."
  value       = google_sql_database_instance.main.public_ip_address
}

output "service_account_email" {
  description = "Cloud Run service account email."
  value       = google_service_account.app.email
}
