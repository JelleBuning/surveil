#!/bin/bash
# =============================================================================
# Stop hook: Scan for secrets in staged AND unstaged changes before session ends.
#
# Hook event: Stop
# Exit code 2 = alert the user about leaked secrets.
#
# Scans git changes (both staged and unstaged) for:
#   - Cloud provider keys (AWS, GCP, Azure)
#   - AI/API keys (OpenAI, Anthropic, HuggingFace, Replicate, Databricks,
#                   Together AI, Groq, Fireworks, Cohere, Mistral)
#   - Platform tokens (GitHub, GitLab, Slack, Stripe, Vercel, Supabase, Notion)
#   - SSH/PGP private keys
#   - Database connection strings with embedded passwords
#   - Hardcoded passwords, secrets, and tokens in config files
#   - Generic high-entropy strings that look like API keys
#
# Also checks for accidentally added sensitive files (.env, .pem, etc.)
#
# Customize:
#   - Add regex patterns to PATTERNS array for company-specific secrets
#   - Add filenames to SENSITIVE_FILES for project-specific files
#   - Set CLAUDE_EXTRA_SECRET_PATTERNS env var (newline-separated regexes)
# =============================================================================

set -euo pipefail

# --- Only run inside a git repo ---
git rev-parse --git-dir > /dev/null 2>&1 || exit 0

# --- Gather changed files (staged + unstaged) ---
STAGED=$(git diff --cached --name-only 2>/dev/null || true)
UNSTAGED=$(git diff --name-only 2>/dev/null || true)
UNTRACKED=$(git ls-files --others --exclude-standard 2>/dev/null || true)

ALL_CHANGED=$(echo -e "${STAGED}\n${UNSTAGED}\n${UNTRACKED}" | sort -u | grep -v '^$' || true)
[ -z "$ALL_CHANGED" ] && exit 0

# --- Secret patterns (regex) ---
PATTERNS=(
  # AWS
  "AKIA[0-9A-Z]{16}"
  "ASIA[0-9A-Z]{16}"
  "aws_secret_access_key\s*=\s*[A-Za-z0-9/+=]{40}"

  # GCP
  "\"type\":\s*\"service_account\""
  "AIza[0-9A-Za-z_-]{35}"

  # Azure
  "DefaultEndpointsProtocol=https;AccountName="

  # OpenAI
  "sk-[a-zA-Z0-9]{20}T3BlbkFJ[a-zA-Z0-9]{20}"
  "sk-proj-[a-zA-Z0-9_-]{40,}"

  # Anthropic
  "sk-ant-[a-zA-Z0-9_-]{40,}"

  # GitHub
  "ghp_[a-zA-Z0-9]{36}"
  "gho_[a-zA-Z0-9]{36}"
  "ghs_[a-zA-Z0-9]{36}"
  "ghu_[a-zA-Z0-9]{36}"
  "github_pat_[a-zA-Z0-9]{22}_[a-zA-Z0-9]{59}"

  # GitLab
  "glpat-[a-zA-Z0-9_-]{20,}"

  # Slack
  "xoxb-[0-9]{10,}-[0-9]{10,}-[a-zA-Z0-9]{24}"
  "xoxp-[0-9]{10,}-[0-9]{10,}-[a-zA-Z0-9]{24}"
  "xoxe.xoxp-[0-9]+-[0-9]+"

  # Stripe
  "sk_live_[a-zA-Z0-9]{24,}"
  "rk_live_[a-zA-Z0-9]{24,}"
  "pk_live_[a-zA-Z0-9]{24,}"

  # Vercel
  "vercel_[a-zA-Z0-9]{24,}"

  # Supabase
  "sbp_[a-zA-Z0-9]{40,}"
  "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\.[a-zA-Z0-9_-]{50,}"

  # Notion
  "ntn_[a-zA-Z0-9]{40,}"
  "secret_[a-zA-Z0-9]{40,}"

  # HuggingFace
  "hf_[a-zA-Z0-9]{30,}"

  # Replicate
  "r8_[a-zA-Z0-9]{30,}"

  # Databricks
  "dapi[a-f0-9]{32}"
  "dapi[a-f0-9]{32}-[0-9]+"

  # Cohere (no stable public prefix; match common env-var usage)
  "COHERE_API_KEY\s*[=:]\s*['\"]?[A-Za-z0-9]{32,}"

  # Mistral (no stable public prefix; match common env-var usage)
  "MISTRAL_API_KEY\s*[=:]\s*['\"]?[A-Za-z0-9]{32,}"

  # Together AI
  "tgp_v1_[a-zA-Z0-9_-]{40,}"

  # Groq
  "gsk_[a-zA-Z0-9]{40,}"

  # Fireworks
  "fw_[a-zA-Z0-9]{24,}"

  # Twilio
  "SK[a-f0-9]{32}"

  # SendGrid
  "SG\.[a-zA-Z0-9_-]{22}\.[a-zA-Z0-9_-]{43}"

  # Telegram bot token
  "[0-9]{8,10}:[a-zA-Z0-9_-]{35}"

  # SSH / PGP private keys
  "-----BEGIN RSA PRIVATE KEY-----"
  "-----BEGIN OPENSSH PRIVATE KEY-----"
  "-----BEGIN EC PRIVATE KEY-----"
  "-----BEGIN DSA PRIVATE KEY-----"
  "-----BEGIN PGP PRIVATE KEY BLOCK-----"
  "-----BEGIN PRIVATE KEY-----"

  # Database connection strings with passwords
  "postgres://[^:]+:[^@]+@"
  "mysql://[^:]+:[^@]+@"
  "mongodb://[^:]+:[^@]+@"
  "redis://:[^@]+@"
  "DATABASE_URL=.*://.*:.*@"

  # Generic hardcoded secrets in config
  "password\s*[=:]\s*['\"][^'\"]{8,}"
  "secret\s*[=:]\s*['\"][^'\"]{8,}"
  "api_key\s*[=:]\s*['\"][^'\"]{8,}"
  "apikey\s*[=:]\s*['\"][^'\"]{8,}"
  "api_secret\s*[=:]\s*['\"][^'\"]{8,}"
  "access_token\s*[=:]\s*['\"][^'\"]{8,}"
  "auth_token\s*[=:]\s*['\"][^'\"]{8,}"
  "private_key\s*[=:]\s*['\"][^'\"]{8,}"
)

# --- Sensitive filenames that should never be committed ---
SENSITIVE_FILES=(
  ".env" ".env.local" ".env.production" ".env.staging" ".env.development"
  ".env.test" ".env.backup"
  "id_rsa" "id_ed25519" "id_ecdsa" "id_dsa"
  ".npmrc" ".pypirc" ".netrc" ".htpasswd"
  "credentials.json" "service-account.json"
  "secrets.json" "secrets.yml" "secrets.yaml"
  ".git-credentials"
)

# --- Add user-defined patterns from env var ---
if [ -n "${CLAUDE_EXTRA_SECRET_PATTERNS:-}" ]; then
  while IFS= read -r pat; do
    [ -n "$pat" ] && PATTERNS+=("$pat")
  done <<< "$CLAUDE_EXTRA_SECRET_PATTERNS"
fi

FOUND=0
ISSUES=""

# --- Check for sensitive filenames in changes ---
for FILE in $ALL_CHANGED; do
  BASENAME=$(basename "$FILE")
  for SENSITIVE in "${SENSITIVE_FILES[@]}"; do
    if [ "$BASENAME" = "$SENSITIVE" ]; then
      ISSUES="${ISSUES}\n  SENSITIVE FILE: ${FILE} should not be committed"
      FOUND=1
    fi
  done
done

# --- Check file contents for secret patterns ---
for FILE in $ALL_CHANGED; do
  # Skip binary files and files that don't exist
  [ -f "$FILE" ] || continue
  file "$FILE" 2>/dev/null | grep -q "text" || continue

  # Skip node_modules, vendor, dist (shouldn't be committed but just in case).
  # Also skip this hook itself -- the PATTERNS array contains the very strings
  # it hunts for, which would always self-match.
  case "$FILE" in
    node_modules/*|vendor/*|dist/*|.git/*) continue ;;
    */verify-no-secrets.sh|verify-no-secrets.sh|hooks/verify-no-secrets.sh) continue ;;
  esac

  for PAT in "${PATTERNS[@]}"; do
    if grep -qE "$PAT" "$FILE" 2>/dev/null; then
      # Get the matching line (truncated) for context
      MATCH=$(grep -nE "$PAT" "$FILE" 2>/dev/null | head -1 | cut -c1-120)
      ISSUES="${ISSUES}\n  SECRET in ${FILE}: ${MATCH}..."
      FOUND=1
      break  # One match per file is enough
    fi
  done
done

# --- Report ---
if [ "$FOUND" -eq 1 ]; then
  echo "" >&2
  echo "=== SECRET SCAN FOUND ISSUES ===" >&2
  echo -e "$ISSUES" >&2
  echo "" >&2
  echo "Review these files before committing." >&2
  echo "If these are false positives (e.g., test fixtures), add them to .gitignore." >&2
  echo "==================================" >&2
  exit 2
fi

exit 0
