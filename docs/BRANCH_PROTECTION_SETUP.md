# Branch Protection Setup Guide

This guide walks you through setting up branch protection rules on GitHub to enforce your workflow and maintain code quality.

## 📚 What is Branch Protection?

Branch protection rules prevent direct pushes to important branches (like `main` and `development`) and enforce quality checks before merging pull requests.

## 🎯 Why Set Up Branch Protection?

- ✅ **Prevents Accidents**: No one can accidentally push directly to `main`
- ✅ **Enforces Reviews**: Requires code review before merging
- ✅ **Quality Gates**: Ensures all tests pass before merge
- ✅ **Maintains Workflow**: Enforces your development → main workflow
- ✅ **Audit Trail**: All changes go through documented PRs

## 🔧 Setup Instructions

### Step 1: Access Branch Protection Settings

1. Go to your repository on GitHub: `https://github.com/nimritagames/Unity-FlowUI`
2. Click **Settings** (top menu)
3. Click **Branches** (left sidebar)
4. Under "Branch protection rules", click **Add rule**

### Step 2: Protect `main` Branch

#### Rule Configuration

**Branch name pattern**: `main`

#### Settings to Enable

##### Protect matching branches
- ✅ **Require a pull request before merging**
  - ✅ Require approvals: `1` (at least one approval)
  - ✅ Dismiss stale pull request approvals when new commits are pushed
  - ⬜ Require review from Code Owners (enable if you add CODEOWNERS file)

- ✅ **Require status checks to pass before merging**
  - ✅ Require branches to be up to date before merging
  - **Add required status checks** (these will appear after first PR runs):
    - `⚡ Quick Validation`
    - `🧪 Unity Tests`
    - `📝 Code Quality`
    - `📋 PR Summary`

- ✅ **Require conversation resolution before merging**
  - Ensures all PR comments are addressed

- ✅ **Require signed commits** (optional but recommended)
  - Adds extra security verification

- ✅ **Require linear history**
  - Prevents merge commits, keeps history clean

- ✅ **Include administrators**
  - Even repo admins must follow these rules

- ✅ **Restrict who can push to matching branches**
  - Select specific users/teams who can merge (usually just maintainers)
  - Or leave empty to allow all with approval

- ✅ **Allow force pushes** → **DISABLE**
  - Prevents destructive force pushes

- ✅ **Allow deletions** → **DISABLE**
  - Prevents accidental branch deletion

#### Click "Create" to Save

### Step 3: Protect `development` Branch

Now repeat the process for the `development` branch:

**Branch name pattern**: `development`

#### Settings to Enable

##### Protect matching branches
- ✅ **Require a pull request before merging**
  - ✅ Require approvals: `1`
  - ✅ Dismiss stale pull request approvals when new commits are pushed

- ✅ **Require status checks to pass before merging**
  - ✅ Require branches to be up to date before merging
  - **Add required status checks**:
    - `⚡ Quick Validation`
    - `🧪 Unity Tests`
    - `📝 Code Quality`

- ✅ **Require conversation resolution before merging**

- ✅ **Require linear history**

- ✅ **Include administrators**

- ✅ **Allow force pushes** → **DISABLE**

- ✅ **Allow deletions** → **DISABLE**

#### Click "Create" to Save

## 📋 Summary of Protection Rules

### `main` Branch
```
✅ Requires PR with 1 approval
✅ All status checks must pass
✅ Branch must be up-to-date
✅ All conversations resolved
✅ Linear history (no merge commits)
✅ Applies to administrators
❌ No force pushes
❌ No deletions
```

### `development` Branch
```
✅ Requires PR with 1 approval
✅ All status checks must pass
✅ Branch must be up-to-date
✅ All conversations resolved
✅ Linear history
✅ Applies to administrators
❌ No force pushes
❌ No deletions
```

## 🔄 Your Workflow After Setup

### ✅ Allowed
```bash
# Create feature branch from development
git checkout development
git pull origin development
git checkout -b feature/my-feature

# Make changes and commit
git add .
git commit -m "feat: Add new feature"
git push origin feature/my-feature

# Create PR on GitHub: feature/my-feature → development
# After approval and checks pass, merge via GitHub UI
```

### ❌ Blocked (These will be prevented!)
```bash
# Direct push to main (BLOCKED!)
git checkout main
git commit -m "Direct commit"
git push origin main  # ❌ ERROR: Protected branch

# Direct push to development (BLOCKED!)
git checkout development
git commit -m "Direct commit"
git push origin development  # ❌ ERROR: Protected branch

# Force push (BLOCKED!)
git push --force origin main  # ❌ ERROR: Force push not allowed
```

## 🎯 How to Work With Protection

### Making Changes

1. **Always work on feature branches**
   ```bash
   git checkout development
   git checkout -b feature/my-new-feature
   ```

2. **Push your feature branch**
   ```bash
   git push origin feature/my-new-feature
   ```

3. **Create Pull Request on GitHub**
   - Go to GitHub
   - Click "New Pull Request"
   - Base: `development` ← Compare: `feature/my-new-feature`
   - Fill out PR template
   - Submit for review

4. **Wait for Checks and Approval**
   - GitHub Actions will run automated tests
   - Reviewer will approve (or request changes)
   - All status checks must pass ✅

5. **Merge via GitHub UI**
   - Once approved and checks pass, click "Merge Pull Request"
   - Delete the feature branch after merge

### Releasing to Main

1. **Bump version in package.json**
   ```bash
   git checkout development
   # Edit package.json, change version 1.0.0 → 1.1.0
   git add Packages/com.nimrita.flowui/package.json
   git commit -m "chore: Bump version to 1.1.0"
   git push origin development
   ```

2. **Create PR: development → main**
   - Go to GitHub
   - Create PR from `development` to `main`
   - Title: "Release v1.1.0"
   - Wait for approval and checks

3. **Merge and Release**
   - Merge PR on GitHub
   - GitHub Actions automatically creates release!

## 🚨 Troubleshooting

### "Push declined due to repository rule violations"

**Cause**: You're trying to push directly to a protected branch.

**Solution**:
```bash
# Create a feature branch instead
git checkout -b feature/my-changes

# Push the feature branch
git push origin feature/my-changes

# Then create a PR on GitHub
```

### "Required status check is expected"

**Cause**: A required check hasn't run yet or failed.

**Solution**:
- Wait for GitHub Actions to complete
- If checks fail, fix the issues and push again
- Check the Actions tab for details

### "Review required before merging"

**Cause**: No one has approved your PR yet.

**Solution**:
- Wait for a maintainer to review
- Address any review feedback
- Once approved, you can merge

### "Branch is out of date"

**Cause**: The base branch has new commits since you created your PR.

**Solution**:
```bash
# Update your feature branch
git checkout feature/my-feature
git fetch origin
git rebase origin/development  # or origin/main

# Force push (this is allowed on feature branches!)
git push --force-with-lease origin feature/my-feature
```

## 📚 Additional Resources

- [GitHub Docs: Branch Protection](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [GitHub Docs: Status Checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks)
- [Contributing Guide](../CONTRIBUTING.md) - Your workflow documentation

## ✅ Verification

After setting up, verify the protection works:

1. **Try to push to main directly** (should fail)
   ```bash
   git checkout main
   echo "test" > test.txt
   git add test.txt
   git commit -m "test"
   git push origin main  # Should be blocked!
   ```

2. **Create a test PR**
   - Create a feature branch
   - Push it
   - Open PR to `development`
   - Verify status checks run
   - Verify you can't merge without approval

If both work as expected, your branch protection is configured correctly! 🎉

---

**Need Help?** Open an issue on GitHub or ask in Discussions.
