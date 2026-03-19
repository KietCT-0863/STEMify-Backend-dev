# 🔥 Next.js Authorization Code Flow + PKCE Integration

## 🎯 **Quy Trình Chuẩn - Authorization Code Flow với PKCE**

Đây là **industry standard** cho SPA (Single Page Applications) và được recommend bởi OAuth 2.1 specification.

### ** Tại Sao Authorization Code Flow + PKCE?**

| Aspect | Password Flow | Authorization Code + PKCE |
|--------|---------------|---------------------------|
| **Security** | Medium |  Highest |
| **Frontend Access Password** | Yes |  No |
| **CSRF Protection** | Limited |  Built-in |
| **Recommended By** | Legacy |  OAuth 2.1 & OIDC |
| **Production Ready** | Conditional |  Always |

## **Complete Flow Diagram**

```
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────┐
│   Next.js SPA   │    │   Identity Server    │    │   PostgreSQL    │
│  (Port 3000)    │    │   (Port 5001)        │    │   (Port 5432)   │
└─────────────────┘    └──────────────────────┘    └─────────────────┘
         │                         │                         │
    1. User clicks                 │                         │
       "Login" button              │                         │
         │                         │                         │
    2. Generate PKCE               │                         │
       code_verifier &             │                         │
       code_challenge              │                         │
         │                         │                         │
    3. Redirect to                 │                         │
   ──────/connect/authorize──────▶│                         │
       + code_challenge            │                         │
         │                         │                         │
         │                    4. Show login UI               │
         │                       (Razor page)                │
         │                         │                         │
         │                    5. User enters                 │
         │                       credentials ────────────────▶│ Validate
         │                         │                         │ user
         │                         │◄────────────────────────│
         │                         │                         │
    6. Redirect back with          │                         │
   ◄─────authorization_code────────│                         │
         │                         │                         │
    7. Exchange code for           │                         │
       tokens with PKCE            │                         │
   ──────POST /connect/token───────▶│                         │
       + code_verifier              │                         │
         │                         │                         │
    8. Return access_token &        │                         │
   ◄─────id_token──────────────────│                         │
         │                         │                         │
    9. Store tokens &              │                         │
       redirect to dashboard       │                         │
         │                         │                         │
   10. API calls with              │                         │
   ──────Bearer token──────────────▶│                         │
         │                         │                         │
   11. Validate token &            │                         │
   ◄─────return data───────────────│                         │
```

## **Implementation Guide**

### **1. Install Dependencies**

```bash
npm install @auth0/nextjs-auth0
# OR
npm install next-auth
# OR custom implementation with:
npm install axios crypto-js
```

### **2. Custom Implementation (Recommended for Learning)**

#### **Auth Service with PKCE:**

```typescript
// lib/auth-pkce.ts
import crypto from 'crypto'

interface AuthConfig {
  clientId: string
  authorizeUrl: string
  tokenUrl: string
  redirectUri: string
  scope: string
}

interface TokenResponse {
  access_token: string
  id_token: string
  refresh_token?: string
  token_type: string
  expires_in: number
}

export class PKCEAuthService {
  private config: AuthConfig

  constructor() {
    this.config = {
      clientId: 'stemify-nextjs-client',
      authorizeUrl: 'https://localhost:5001/connect/authorize',
      tokenUrl: 'https://localhost:5001/connect/token',
      redirectUri: 'http://localhost:3000/auth/callback',
      scope: 'openid profile email roles api'
    }
  }

  /**
   * Step 1-3: Generate PKCE và redirect đến authorization server
   */
  async initiateLogin(): Promise<void> {
    // Generate PKCE parameters
    const codeVerifier = this.generateCodeVerifier()
    const codeChallenge = await this.generateCodeChallenge(codeVerifier)
    const state = this.generateState()

    // Store in sessionStorage để dùng sau
    sessionStorage.setItem('code_verifier', codeVerifier)
    sessionStorage.setItem('auth_state', state)

    // Build authorization URL
    const authUrl = new URL(this.config.authorizeUrl)
    authUrl.searchParams.set('response_type', 'code')
    authUrl.searchParams.set('client_id', this.config.clientId)
    authUrl.searchParams.set('redirect_uri', this.config.redirectUri)
    authUrl.searchParams.set('scope', this.config.scope)
    authUrl.searchParams.set('state', state)
    authUrl.searchParams.set('code_challenge', codeChallenge)
    authUrl.searchParams.set('code_challenge_method', 'S256')

    // Redirect user to authorization server
    window.location.href = authUrl.toString()
  }

  /**
   * 🎫 Step 6-8: Handle callback và exchange code for tokens
   */
  async handleCallback(
    code: string, 
    state: string
  ): Promise<TokenResponse> {
    // Verify state parameter
    const storedState = sessionStorage.getItem('auth_state')
    if (state !== storedState) {
      throw new Error('Invalid state parameter')
    }

    // Get stored code_verifier
    const codeVerifier = sessionStorage.getItem('code_verifier')
    if (!codeVerifier) {
      throw new Error('Code verifier not found')
    }

    // Exchange authorization code for tokens
    const response = await fetch(this.config.tokenUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: new URLSearchParams({
        grant_type: 'authorization_code',
        client_id: this.config.clientId,
        code: code,
        redirect_uri: this.config.redirectUri,
        code_verifier: codeVerifier
      })
    })

    if (!response.ok) {
      const error = await response.text()
      throw new Error(`Token exchange failed: ${error}`)
    }

    const tokenData: TokenResponse = await response.json()

    // Clean up temporary storage
    sessionStorage.removeItem('code_verifier')
    sessionStorage.removeItem('auth_state')

    // Store tokens securely
    this.storeTokens(tokenData)

    return tokenData
  }

  /**
   * Refresh token
   */
  async refreshToken(): Promise<TokenResponse | null> {
    const refreshToken = localStorage.getItem('refresh_token')
    if (!refreshToken) return null

    try {
      const response = await fetch(this.config.tokenUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: new URLSearchParams({
          grant_type: 'refresh_token',
          client_id: this.config.clientId,
          refresh_token: refreshToken
        })
      })

      if (!response.ok) {
        this.logout()
        return null
      }

      const tokenData: TokenResponse = await response.json()
      this.storeTokens(tokenData)
      return tokenData
    } catch {
      this.logout()
      return null
    }
  }

  /**
   * 🚪 Logout
   */
  logout(): void {
    localStorage.removeItem('access_token')
    localStorage.removeItem('id_token')
    localStorage.removeItem('refresh_token')
    
    // Redirect to Identity Server logout
    const logoutUrl = new URL('https://localhost:5001/connect/endsession')
    logoutUrl.searchParams.set('post_logout_redirect_uri', 'http://localhost:3000')
    window.location.href = logoutUrl.toString()
  }

  /**
   * 🎫 Get access token
   */
  getAccessToken(): string | null {
    return localStorage.getItem('access_token')
  }

  /**
   * 🆔 Get user info from ID token
   */
  getUserInfo(): any | null {
    const idToken = localStorage.getItem('id_token')
    if (!idToken) return null

    try {
      // Decode JWT payload (base64)
      const payload = JSON.parse(atob(idToken.split('.')[1]))
      return payload
    } catch {
      return null
    }
  }

  /**
   *  Check if authenticated
   */
  isAuthenticated(): boolean {
    const token = this.getAccessToken()
    if (!token) return false

    try {
      const payload = JSON.parse(atob(token.split('.')[1]))
      return payload.exp * 1000 > Date.now()
    } catch {
      return false
    }
  }

  // Private helper methods
  private generateCodeVerifier(): string {
    return crypto.randomBytes(32).toString('base64url')
  }

  private async generateCodeChallenge(verifier: string): Promise<string> {
    const encoder = new TextEncoder()
    const data = encoder.encode(verifier)
    const digest = await crypto.subtle.digest('SHA-256', data)
    return btoa(String.fromCharCode(...new Uint8Array(digest)))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '')
  }

  private generateState(): string {
    return crypto.randomBytes(16).toString('hex')
  }

  private storeTokens(tokens: TokenResponse): void {
    localStorage.setItem('access_token', tokens.access_token)
    localStorage.setItem('id_token', tokens.id_token)
    if (tokens.refresh_token) {
      localStorage.setItem('refresh_token', tokens.refresh_token)
    }
  }
}

export const authService = new PKCEAuthService()
```

#### **Login Page Component:**

```typescript
// pages/login.tsx
import { useEffect } from 'react'
import { authService } from '@/lib/auth-pkce'

export default function LoginPage() {
  useEffect(() => {
    // Check if user is already authenticated
    if (authService.isAuthenticated()) {
      window.location.href = '/dashboard'
      return
    }
  }, [])

  const handleLogin = async () => {
    try {
      await authService.initiateLogin()
      // User will be redirected to Identity Server
    } catch (error) {
      console.error('Login failed:', error)
    }
  }

  return (
    <div className="login-container">
      <div className="login-card">
        {/* Left Side - Login Form */}
        <div className="login-left">
          <div className="brand">
            <div className="logo">
              <div className="logo-svg">S</div>
              <div>
                <h1>STEMify</h1>
                <p>Education Platform</p>
              </div>
            </div>
          </div>

          <div className="login-form">
            <button 
              onClick={handleLogin}
              className="btn-login"
            >
              Sign In with STEMify
            </button>

            <div className="divider">
              <span>Secure OAuth 2.0 Authentication</span>
            </div>

            {/* Test Accounts Info */}
            <div className="test-accounts">
              <div className="test-title">TEST ACCOUNTS:</div>
              <div className="test-list">
                <strong>Admin:</strong> admin@stemify.com / Admin123!<br/>
                <strong>Teacher:</strong> teacher@stemify.com / Teacher123!<br/>
                <strong>Student:</strong> student@stemify.com / Student123!
              </div>
            </div>
          </div>
        </div>

        {/* Right Side - Hero Section */}
        <div className="login-right">
          <div className="hero-content">
            <h2>STEMify</h2>
            <h3>Platform</h3>
            <p>Theory meets practice inspiring<br/>for future generations</p>
            
            {/* Security badges */}
            <div className="security-badges">
              <div className="badge">OAuth 2.1</div>
              <div className="badge">🛡️ PKCE</div>
              <div className="badge"> Secure</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
```

#### **Callback Handler:**

```typescript
// pages/auth/callback.tsx
import { useEffect, useState } from 'react'
import { useRouter } from 'next/router'
import { authService } from '@/lib/auth-pkce'

export default function AuthCallback() {
  const router = useRouter()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    handleCallback()
  }, [router.query])

  const handleCallback = async () => {
    try {
      const { code, state, error: authError } = router.query

      if (authError) {
        throw new Error(`Authentication failed: ${authError}`)
      }

      if (!code || !state) {
        throw new Error('Missing authorization code or state')
      }

      // Exchange code for tokens
      await authService.handleCallback(
        code as string, 
        state as string
      )

      // Redirect to dashboard
      router.push('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Authentication failed')
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="callback-container">
        <div className="loading">
          <div className="spinner"></div>
          <h2>Completing sign-in...</h2>
          <p>Please wait while we authenticate you.</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="callback-container">
        <div className="error">
          <h2>Authentication Failed</h2>
          <p>{error}</p>
          <button onClick={() => router.push('/login')}>
            Try Again
          </button>
        </div>
      </div>
    )
  }

  return null
}
```

#### **Protected Dashboard:**

```typescript
// pages/dashboard.tsx
import { useEffect, useState } from 'react'
import { useRouter } from 'next/router'
import { authService } from '@/lib/auth-pkce'

export default function Dashboard() {
  const router = useRouter()
  const [user, setUser] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    checkAuth()
  }, [])

  const checkAuth = () => {
    if (!authService.isAuthenticated()) {
      router.push('/login')
      return
    }

    const userInfo = authService.getUserInfo()
    setUser(userInfo)
    setLoading(false)
  }

  const handleLogout = () => {
    authService.logout()
  }

  if (loading) {
    return <div>Loading...</div>
  }

  return (
    <div className="dashboard">
      <header>
        <h1>Welcome to STEMify Dashboard</h1>
        <div className="user-info">
          <span>Hello, {user?.name || user?.email}</span>
          <span>Role: {user?.role}</span>
          <button onClick={handleLogout}>Logout</button>
        </div>
      </header>

      <main>
        <h2>Your Learning Journey</h2>
        {/* Dashboard content */}
      </main>
    </div>
  )
}
```

### **3. Environment Configuration**

```bash
# .env.local
NEXT_PUBLIC_IDENTITY_SERVER_URL=https://localhost:5001
NEXT_PUBLIC_CLIENT_ID=stemify-nextjs-client
NEXT_PUBLIC_REDIRECT_URI=http://localhost:3000/auth/callback
```

### **4. CSS for Callback Page**

```css
/* styles/callback.css */
.callback-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.loading, .error {
  text-align: center;
  background: white;
  padding: 40px;
  border-radius: 20px;
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
  max-width: 400px;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #f3f3f3;
  border-top: 4px solid #667eea;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin: 0 auto 20px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.security-badges {
  display: flex;
  gap: 10px;
  justify-content: center;
  margin-top: 20px;
}

.badge {
  background: rgba(255, 255, 255, 0.2);
  padding: 5px 12px;
  border-radius: 20px;
  font-size: 12px;
  color: white;
}
```

##  **Testing the Flow**

### **1. Manual Testing:**
1. Go to `http://localhost:3000/login`
2. Click "Sign In with STEMify"
3. You'll be redirected to `https://localhost:5001/connect/authorize`
4. Login with test credentials
5. Get redirected back to `/auth/callback`
6. Finally redirected to `/dashboard`

### **2. URL Inspection:**
```
Authorization Request:
https://localhost:5001/connect/authorize?
  response_type=code&
  client_id=stemify-nextjs-client&
  redirect_uri=http://localhost:3000/auth/callback&
  scope=openid profile email roles api&
  state=abc123&
  code_challenge=xyz789&
  code_challenge_method=S256

Callback URL:
http://localhost:3000/auth/callback?
  code=authorization_code_here&
  state=abc123
```

## **Security Benefits**

1. **🛡️ No Password Exposure:** Frontend never sees user credentials
2. **PKCE Protection:** Prevents authorization code interception
3. **🎯 State Parameter:** CSRF protection
4. **⏰ Short-lived Codes:** Authorization codes expire quickly
5. **Refresh Tokens:** Secure token renewal

## **Production Deployment**

### **Update Redirect URIs:**
```typescript
// Update in SeedDataDefinition.cs
RedirectUris = {
  new Uri("https://app.stemify.com/auth/callback"), // Production
  new Uri("https://staging.stemify.com/auth/callback") // Staging
}
```

### **Environment Variables:**
```bash
# Production .env
NEXT_PUBLIC_IDENTITY_SERVER_URL=https://identity.stemify.com
NEXT_PUBLIC_CLIENT_ID=stemify-nextjs-client
NEXT_PUBLIC_REDIRECT_URI=https://app.stemify.com/auth/callback
```

## 📋 **Comparison: Flows Summary**

| Step | Password Flow | Authorization Code + PKCE |
|------|---------------|----------------------------|
| **User Experience** | Form in SPA | Redirect to IdServer |
| **Security** | Medium |  High |
| **Implementation** | Simple |  Standard |
| **Production Ready** | Conditional |  Always |

---

**💡 Kết luận:** Authorization Code Flow + PKCE là **industry standard** và **cách chuẩn nhất** cho SPA applications. Flow này đảm bảo security cao nhất và được recommend bởi OAuth 2.1 specification! 🔥 