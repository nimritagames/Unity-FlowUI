# GitHub Actions CI/CD Learning Guide

Welcome to your complete guide for understanding GitHub Actions and CI/CD for Unity packages! This guide will take you from zero to hero. 🚀

## Table of Contents

- [What is CI/CD?](#what-is-cicd)
- [What is GitHub Actions?](#what-is-github-actions)
- [Understanding Our Workflows](#understanding-our-workflows)
- [Setting Up Unity License](#setting-up-unity-license)
- [Monitoring Workflows](#monitoring-workflows)
- [Troubleshooting](#troubleshooting)
- [Advanced Topics](#advanced-topics)

## What is CI/CD?

### CI - Continuous Integration

**Continuous Integration** means automatically testing your code every time you make changes.

**Example without CI:**
```
1. You write code
2. You push to GitHub
3. Someone else pulls your code
4. Their Unity project breaks! 💥
5. Hours wasted finding the bug
```

**Example with CI:**
```
1. You write code
2. You push to GitHub
3. GitHub Actions automatically runs tests
4. You get instant feedback: ✅ Pass or ❌ Fail
5. Fix bugs BEFORE anyone else sees them!
```

### CD - Continuous Deployment/Delivery

**Continuous Deployment** means automatically releasing your code when it's ready.

**Example without CD:**
```
1. You finish a feature
2. You manually create a release
3. You manually write changelog
4. You manually tag the version
5. You manually upload package
6. This takes 30+ minutes! 😓
```

**Example with CD:**
```
1. You finish a feature
2. Merge to main branch
3. GitHub Actions automatically:
   - Creates release
   - Generates changelog
   - Tags version
   - Uploads package
4. All in under 5 minutes! 🎉
```

## What is GitHub Actions?

GitHub Actions is GitHub's built-in automation system. Think of it as robots that work for you 24/7!

### Key Concepts

#### 1. Workflow
A workflow is a YAML file that defines automation tasks.

**Location**: `.github/workflows/*.yml`

**Example**: `development.yml` runs tests when you push code

#### 2. Trigger (on:)
Defines WHEN the workflow runs.

```yaml
on:
  push:
    branches:
      - development  # Run when pushing to development
  pull_request:
    branches:
      - development  # Run when creating PR to development
```

**Common Triggers:**
- `push` - When you push commits
- `pull_request` - When you create/update a PR
- `schedule` - Run on a schedule (e.g., daily)
- `workflow_dispatch` - Manual trigger from GitHub UI

#### 3. Job
A job is a set of steps that run on the same machine.

```yaml
jobs:
  test-unity-package:  # Job ID
    name: 🧪 Run Unity Tests  # Display name
    runs-on: ubuntu-latest     # What OS to use
    steps:
      # Steps go here
```

**Key Points:**
- Jobs run in PARALLEL by default (faster!)
- Each job gets a fresh virtual machine
- Jobs can depend on other jobs (`needs:`)

#### 4. Step
A step is a single task within a job.

```yaml
steps:
  - name: 📥 Checkout Code
    uses: actions/checkout@v4  # Use a pre-made action

  - name: 🎮 Run Tests
    run: echo "Running tests"  # Run a shell command
```

**Two Types of Steps:**
1. **Action** (`uses:`) - Pre-made reusable component
2. **Script** (`run:`) - Custom shell commands

#### 5. Runner
A runner is the virtual machine that executes your workflow.

**Available Runners:**
- `ubuntu-latest` - Linux (fastest, cheapest)
- `windows-latest` - Windows
- `macos-latest` - macOS

**For Unity packages, use**: `ubuntu-latest` (required by GameCI)

#### 6. Secrets
Secrets are encrypted environment variables for sensitive data.

**Example:**
```yaml
env:
  UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
  UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
  UNITY_SERIAL: ${{ secrets.UNITY_SERIAL }}
```

## Understanding Our Workflows

### 1. Development Workflow (`development.yml`)

**Purpose**: Run tests on every push to `development` branch

**When it runs:**
- You push to `development`
- Someone creates a PR to `development`

**What it does:**
1. ✅ Checks out your code
2. ✅ Caches Unity Library (for speed)
3. ✅ Runs Unity tests (EditMode + PlayMode)
4. ✅ Validates package.json
5. ✅ Checks required files exist
6. ✅ Uploads test results

**Time**: ~2-5 minutes (first run: ~10 min, cached: ~2 min)

**View Results**: GitHub Actions tab → "Development Branch Tests"

### 2. Release Workflow (`release.yml`)

**Purpose**: Automatically create releases when merging to `main`

**When it runs:**
- You push to `main` (e.g., merge development → main)
- You push a version tag (e.g., `v1.0.0`)

**What it does:**
1. ✅ Reads version from package.json
2. ✅ Generates changelog from commits
3. ✅ Creates GitHub release
4. ✅ Creates package .zip file
5. ✅ Uploads package to release
6. ✅ Updates CHANGELOG.md

**Time**: ~1-2 minutes

**Result**: Automatic release on GitHub!

### 3. PR Checks Workflow (`pr-checks.yml`)

**Purpose**: Quality gates before merging ANY pull request

**When it runs:**
- Someone creates a PR
- New commits are pushed to PR

**What it does:**
1. ⚡ **Quick Checks** (runs first, fast!)
   - Validate package.json format
   - Check version format
   - Verify required files exist
   - Check for .meta files
   - Look for forbidden patterns

2. 🧪 **Unity Tests** (runs after quick checks)
   - Run all Unity tests
   - Generate coverage report
   - Post results as PR comment

3. 📝 **Code Quality**
   - Check C# naming conventions
   - Validate assembly definitions
   - Check for license headers

4. 📋 **Summary**
   - Overall pass/fail status

**Time**: ~3-7 minutes

**Result**: PR can only merge if ALL checks pass!

## Setting Up Unity License

GitHub Actions needs a Unity license to run your tests. Here's how to set it up:

### Option 1: Personal License (Recommended for Open Source)

#### Step 1: Get Activation File

1. Create `.github/workflows/get-license.yml`:
   ```yaml
   name: Get Unity License
   on: workflow_dispatch
   jobs:
     get-license:
       runs-on: ubuntu-latest
       steps:
         - uses: game-ci/unity-request-activation-file@v2
         - uses: actions/upload-artifact@v3
           with:
             name: Unity_v2022.3.alf
             path: Unity_v2022.3.alf
   ```

2. Push this file to GitHub
3. Go to Actions tab → "Get Unity License" → "Run workflow"
4. Download the `.alf` file from artifacts

#### Step 2: Generate License File

1. Go to https://license.unity3d.com/manual
2. Upload your `.alf` file
3. Select "Unity Personal" (free for open source)
4. Download the `.ulf` license file

#### Step 3: Add to GitHub Secrets

1. Open your `.ulf` file in a text editor
2. Copy the entire contents
3. Go to GitHub: Settings → Secrets and variables → Actions
4. Click "New repository secret"
5. Add these secrets:
   - Name: `UNITY_LICENSE`, Value: [paste entire .ulf contents]
   - Name: `UNITY_EMAIL`, Value: [your Unity email]
   - Name: `UNITY_PASSWORD`, Value: [your Unity password]
   - Name: `UNITY_SERIAL`, Value: [leave empty for Personal]

#### Step 4: Update Workflows

Your workflows are already configured! But verify they use:
```yaml
env:
  UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
  UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
  UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
```

#### Step 5: Test

1. Push a commit to `development`
2. Go to Actions tab
3. Watch your first workflow run! 🎉

### Option 2: Professional License

If you have Unity Pro or Plus:

1. Get your serial key from Unity Dashboard
2. Add to GitHub Secrets:
   - Name: `UNITY_EMAIL`, Value: [your email]
   - Name: `UNITY_PASSWORD`, Value: [your password]
   - Name: `UNITY_SERIAL`, Value: [your serial key]

3. Update workflows to use `UNITY_SERIAL` instead of `UNITY_LICENSE`

### Troubleshooting License Issues

**Error: "Invalid license"**
- Regenerate `.ulf` file
- Ensure entire `.ulf` contents copied to secret
- Check expiration date

**Error: "Too many activations"**
- Unity Personal allows 2 activations
- Deactivate old builds: Unity Hub → Preferences → Licenses → Deactivate

**Error: "License expired"**
- Personal licenses expire yearly
- Regenerate following Step 1-3 above

## Monitoring Workflows

### Where to View Workflows

1. **GitHub Repository** → **Actions tab**
2. You'll see:
   - Workflow runs (left sidebar)
   - Run history (main area)
   - Status badges (✅ passing, ❌ failing)

### Understanding Workflow Status

#### ✅ Success
```
✅ Development Branch Tests
   All tests passed! Code is good to merge.
```

#### ❌ Failure
```
❌ Development Branch Tests
   Some tests failed. Click to see details.
```

#### 🟡 In Progress
```
🟡 Development Branch Tests
   Workflow is currently running...
```

#### ⚫ Cancelled
```
⚫ Development Branch Tests
   Workflow was cancelled (newer run started)
```

### Viewing Detailed Logs

1. Click on a workflow run
2. Click on a job (e.g., "🧪 Run Unity Tests")
3. Expand steps to see detailed logs
4. Look for red ❌ marks indicating errors

### Downloading Artifacts

Some workflows save files (test results, coverage reports):

1. Go to workflow run
2. Scroll to "Artifacts" section
3. Click to download (e.g., "Test Results.zip")

## Troubleshooting

### Common Issues

#### 1. "Unity test runner failed"

**Symptoms:**
```
Error: Test execution failed with exit code 1
```

**Causes:**
- Test failures in your code
- Unity version mismatch
- Missing dependencies

**Solutions:**
1. Run tests locally first: Unity → Window → General → Test Runner
2. Check Unity version matches workflow (2022.3)
3. View detailed logs in Actions tab
4. Fix failing tests and push again

#### 2. "Package validation failed"

**Symptoms:**
```
Error: Missing required files
```

**Causes:**
- Missing package.json
- Missing .asmdef files
- Missing .meta files

**Solutions:**
1. Check `.github/workflows/development.yml` → "Check Required Files"
2. Ensure all required files exist
3. Commit missing files

#### 3. "Workflow doesn't trigger"

**Symptoms:**
- Push code but workflow doesn't run

**Causes:**
- Wrong branch (workflow set for `development`, you pushed to `main`)
- Workflow file syntax error
- `.github/workflows/` path incorrect

**Solutions:**
1. Check you pushed to correct branch
2. Validate YAML syntax: https://www.yamllint.com/
3. Verify path: `.github/workflows/development.yml` (exactly this!)

#### 4. "Cache restore failed"

**Symptoms:**
```
Warning: Cache restore failed
```

**Causes:**
- First run (no cache yet)
- Cache expired (7 days)
- Cache key changed

**Solutions:**
- This is usually just a warning, not an error
- First run will be slower, future runs cached
- Ignore if workflow still completes

#### 5. "API rate limit exceeded"

**Symptoms:**
```
Error: API rate limit exceeded
```

**Causes:**
- Too many workflow runs in short time
- GitHub API limits reached

**Solutions:**
- Wait 1 hour for reset
- Use `concurrency:` to cancel old runs
- Reduce workflow frequency

### Getting Help

1. **Check Workflow Logs** - Most issues show clear error messages
2. **Search Issues** - Look for similar problems in GameCI issues
3. **Ask in Discussions** - Community can help!
4. **Read Documentation** - Links in [Resources](#resources)

## Advanced Topics

### Caching for Speed

**What is caching?**
Caching saves the Unity Library folder between runs, making subsequent runs much faster.

**How it works:**
```yaml
- uses: actions/cache@v3
  with:
    path: Library  # What to cache
    key: Library-${{ hashFiles('package.json') }}  # Cache key
    restore-keys: Library-  # Fallback if exact match not found
```

**Cache invalidation:**
- If `package.json` changes, cache is rebuilt
- Caches expire after 7 days of no use
- Manual: Settings → Actions → Caches → Delete

### Matrix Builds (Testing Multiple Versions)

Test against multiple Unity versions:

```yaml
strategy:
  matrix:
    unity-version:
      - 2022.3
      - 2023.1
      - 2023.2

steps:
  - uses: game-ci/unity-test-runner@v4
    with:
      unityVersion: ${{ matrix.unity-version }}
```

This creates 3 jobs, one for each version!

### Conditional Steps

Run steps only in certain conditions:

```yaml
# Only on main branch
- name: Deploy
  if: github.ref == 'refs/heads/main'
  run: echo "Deploying..."

# Only if tests passed
- name: Upload Coverage
  if: success()
  uses: actions/upload-artifact@v3

# Only if tests failed
- name: Notify Failure
  if: failure()
  run: echo "Tests failed!"

# Always run (even if previous failed)
- name: Cleanup
  if: always()
  run: echo "Cleaning up..."
```

### Workflow Dispatch (Manual Triggers)

Add manual trigger button:

```yaml
on:
  workflow_dispatch:  # Adds "Run workflow" button in UI
    inputs:
      unity-version:
        description: 'Unity version to use'
        required: true
        default: '2022.3'

jobs:
  manual-test:
    runs-on: ubuntu-latest
    steps:
      - run: echo "Testing with Unity ${{ inputs.unity-version }}"
```

### Environment Variables

**Set for entire workflow:**
```yaml
env:
  UNITY_VERSION: 2022.3
  PACKAGE_NAME: com.nimrita.flowui

jobs:
  test:
    steps:
      - run: echo $UNITY_VERSION  # Uses workflow-level env
```

**Set for specific job:**
```yaml
jobs:
  test:
    env:
      TEST_MODE: all
    steps:
      - run: echo $TEST_MODE  # Uses job-level env
```

**Set for specific step:**
```yaml
steps:
  - name: Test
    env:
      LOG_LEVEL: debug
    run: echo $LOG_LEVEL  # Uses step-level env
```

### Status Badges

Add status badges to your README:

```markdown
[![Tests](https://github.com/nimritagames/Unity-FlowUI/actions/workflows/development.yml/badge.svg)](https://github.com/nimritagames/Unity-FlowUI/actions/workflows/development.yml)
```

Result: ![Tests](https://img.shields.io/badge/tests-passing-brightgreen)

## Resources

### Official Documentation
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [GameCI Documentation](https://game.ci/docs/github/getting-started)
- [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)

### Useful Actions
- [actions/checkout](https://github.com/actions/checkout) - Check out repository
- [actions/cache](https://github.com/actions/cache) - Cache dependencies
- [actions/upload-artifact](https://github.com/actions/upload-artifact) - Upload files
- [game-ci/unity-test-runner](https://github.com/game-ci/unity-test-runner) - Run Unity tests

### Learning Resources
- [GitHub Actions Tutorial](https://docs.github.com/en/actions/learn-github-actions/understanding-github-actions)
- [GameCI Getting Started](https://game.ci/docs/github/getting-started)
- [YAML Syntax Guide](https://learnxinyminutes.com/docs/yaml/)

## Next Steps

1. ✅ Set up Unity license (see above)
2. ✅ Push a commit and watch your first workflow run
3. ✅ Set up branch protection (see BRANCH_PROTECTION_SETUP.md)
4. ✅ Create your first pull request
5. ✅ Make your first release!

---

**Questions?** Open an issue or discussion on GitHub! Happy automating! 🚀
