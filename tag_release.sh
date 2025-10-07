#!/bin/bash

# --- Configuration ---
PROJECT_FILE="CrocMadame.csproj"
GIT_REMOTE="origin"
MAIN_BRANCH="main" # Or 'master', depending on your repository's main branch name

# --- Functions ---

# Function to get the version from CrocMadame.csproj using sed
get_project_version() {
  if [ ! -f "$PROJECT_FILE" ]; then
    echo "Error: $PROJECT_FILE not found in the current directory."
    exit 1
  fi
  # Use sed to extract the version, handling potential whitespace
  sed -n 's/^[[:space:]]*<Version>\([^<]*\)<\/Version>.*/\1/p' "$PROJECT_FILE"
}

# Function to get the latest Git tag
get_latest_git_tag() {
  # Fetch all tags from the remote to ensure we have the latest
  git fetch "$GIT_REMOTE" --tags &> /dev/null

  # Get the latest tag, if any
  LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null)
  echo "$LATEST_TAG"
}

# Function to compare two semantic versions (v1 > v2 returns 0, v1 <= v2 returns 1)
# Returns 0 if version1 is greater than version2
# Returns 1 if version1 is less than or equal to version2
compare_versions() {
  local v1=$1
  local v2=$2

  # Remove 'v' prefix if present for consistent comparison
  v1=${v1#v}
  v2=${v2#v}

  # Use sort -V for semantic version comparison
  if [[ "$(printf '%s\n' "$v1" "$v2" | sort -V | head -n 1)" == "$v2" && "$v1" != "$v2" ]]; then
    return 0 # v1 is greater than v2
  else
    return 1 # v1 is less than or equal to v2
  fi
}

# --- Main Script ---

echo "--- Starting Version Tagging Script ---"

# 1. Get current project version
CURRENT_PROJECT_VERSION=$(get_project_version)
if [ -z "$CURRENT_PROJECT_VERSION" ]; then
  echo "Error: Could not read version from $PROJECT_FILE."
  exit 1
fi
echo "Current project version: $CURRENT_PROJECT_VERSION"

# 2. Get latest Git tag
LATEST_GIT_TAG=$(get_latest_git_tag)
echo "Latest Git tag: ${LATEST_GIT_TAG:-"None found"}"

# 3. Compare versions
if [ -z "$LATEST_GIT_TAG" ]; then
  echo "No existing Git tags found. Creating initial tag."
  SHOULD_TAG=true
else
  if compare_versions "$CURRENT_PROJECT_VERSION" "$LATEST_GIT_TAG"; then
    echo "Project version ($CURRENT_PROJECT_VERSION) is newer than latest Git tag ($LATEST_GIT_TAG)."
    SHOULD_TAG=true
  else
    echo "Project version ($CURRENT_PROJECT_VERSION) is not newer than or equal to latest Git tag ($LATEST_GIT_TAG). No new tag needed."
    SHOULD_TAG=false
  fi
fi

# 4. Create and push tag if needed
if [ "$SHOULD_TAG" = true ]; then
  echo "Creating new tag v$CURRENT_PROJECT_VERSION..."

  # Ensure local branch is up-to-date before tagging
  echo "Fetching latest changes from $GIT_REMOTE/$MAIN_BRANCH..."
  git pull "$GIT_REMOTE" "$MAIN_BRANCH"

  # Check for uncommitted changes
  if ! git diff-index --quiet HEAD --; then
    echo "Warning: You have uncommitted changes. Please commit or stash them before tagging."
    echo "Aborting tagging process."
    exit 1
  fi

  # Create annotated tag
  git tag -a "v$CURRENT_PROJECT_VERSION" -m "Release v$CURRENT_PROJECT_VERSION"

  # Push current branch and the new tag to origin
  echo "Pushing changes and tag v$CURRENT_PROJECT_VERSION to $GIT_REMOTE..."
  git push "$GIT_REMOTE" "$MAIN_BRANCH"
  git push "$GIT_REMOTE" "v$CURRENT_PROJECT_VERSION"

  if [ $? -eq 0 ]; then
    echo "Successfully created and pushed tag v$CURRENT_PROJECT_VERSION."
  else
    echo "Error: Failed to push tag v$CURRENT_PROJECT_VERSION."
    exit 1
  fi
else
  echo "No new tag created."
fi

echo "--- Script Finished ---"