#!/bin/bash

# Cancel redundant builds script for CircleCI
# This script cancels redundant builds when a newer build has been deployed

set -e

DEVELOP_DEPLOY_JOB=$1
APPROVAL_JOB=$2

if [ -z "$DEVELOP_DEPLOY_JOB" ] || [ -z "$APPROVAL_JOB" ]; then
    echo "Usage: $0 <develop_deploy_job> <approval_job>"
    exit 1
fi

# Get the current build number
CURRENT_BUILD_NUM=$(echo $CIRCLE_BUILD_NUM)

# Get the build number that was deployed to develop
DEVELOP_BUILD_NUM=$(curl -s "https://circleci.com/api/v2/project/gh/$CIRCLE_PROJECT_USERNAME/$CIRCLE_PROJECT_REPONAME/pipeline/$CIRCLE_PIPELINE_ID/workflow" \
    -H "Circle-Token: $CIRCLE_TOKEN" | \
    jq -r ".items[] | select(.name == \"deploy-main\") | .id" | \
    head -1)

if [ -z "$DEVELOP_BUILD_NUM" ]; then
    echo "Could not find develop build number"
    exit 0
fi

# Get all builds after the develop deploy
BUILDS_AFTER_DEVELOP=$(curl -s "https://circleci.com/api/v2/project/gh/$CIRCLE_PROJECT_USERNAME/$CIRCLE_PROJECT_REPONAME/pipeline/$CIRCLE_PIPELINE_ID/workflow" \
    -H "Circle-Token: $CIRCLE_TOKEN" | \
    jq -r ".items[] | select(.id > $DEVELOP_BUILD_NUM) | .id")

# Cancel redundant builds
for BUILD_ID in $BUILDS_AFTER_DEVELOP; do
    if [ "$BUILD_ID" != "$CURRENT_BUILD_NUM" ]; then
        echo "Cancelling redundant build: $BUILD_ID"
        curl -X POST "https://circleci.com/api/v2/project/gh/$CIRCLE_PROJECT_USERNAME/$CIRCLE_PROJECT_REPONAME/pipeline/$CIRCLE_PIPELINE_ID/workflow/$BUILD_ID/cancel" \
            -H "Circle-Token: $CIRCLE_TOKEN"
    fi
done

echo "Redundant builds cancelled successfully"
