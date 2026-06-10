# Cau hinh dang nhap Google

Ung dung chi con ho tro dang nhap/dang ky bang Google.

Callback URL dang dung:

```text
http://localhost:5188/signin-google
```

Neu chay bang HTTPS/profile khac, dung dung domain va port dang mo trong trinh duyet, vi du:

```text
https://localhost:7001/signin-google
```

## Google Cloud

1. Vao Google Cloud Console.
2. Vao Google Auth Platform.
3. Tao OAuth client loai Web application.
4. Them Authorized redirect URI: `http://localhost:5188/signin-google`.
5. Lay `Client ID` va `Client secret`.
6. Luu vao User Secrets:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:Google:ClientSecret" "GOOGLE_CLIENT_SECRET"
```

Kiem tra:

```powershell
dotnet user-secrets list
```

Chay app:

```powershell
dotnet run --urls http://localhost:5188
```
