# R2 Configuration Update

## Required Environment Variable

Add this to your docker-compose.yml or environment configuration:

```yaml
R2__PublicDomain: "https://pub-3a191cd2852f498db95051185edec726.r2.dev"
```

## Steps to Update

1. Edit docker-compose.yml and add the environment variable to resource-service
2. Rebuild and restart the service:
   ```bash
   docker compose build --no-cache resource-service
   docker compose up -d resource-service
   ```

3. Delete old LessonAssets:
   ```bash
   docker exec -it stemify-postgres psql -U postgres -d stemify_resource -c "DELETE FROM \"LessonAssets\" WHERE \"LessonId\" IN (29, 30);"
   ```

4. Re-upload the PPTX files via API

5. Test export again

## Verify Public URL

Test if the new URL works:
```bash
curl -I "https://pub-3a191cd2852f498db95051185edec726.r2.dev/documents/test.pptx"
```

Should return 200 OK (or 404 if file doesn't exist, but not 401).
