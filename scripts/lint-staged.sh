#!/bin/bash
set +e

function join_by { local IFS="$1"; shift; echo "$*"; }

files=$(join_by ';' "$@")
commit_hash=$(git log -1 --pretty="%H")
results=./test-results/lint/results.${commit_hash}.xml

if [ ! -z "$files" ]
then
    OPTS="--include=${files}"
fi
rm -f ${results}
dotnet jb inspectcode ${SLN_FILE} -o=./${results} ${OPTS}
dotnet jb cleanupcode ${SLN_FILE} ${OPTS}
./scripts/validate-lint.sh ${results}

IsValid=$?

if [ $IsValid -eq 0 ]; then
 rm ${results}
fi

exit $IsValid