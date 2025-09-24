#!/bin/bash

Results="./test-results/lint/results.xml"
if [[ ! -z "$1" ]]; then
  Results=$1
fi

HasError=$(grep "<Issues />" ${Results})

if [ -z "$HasError" ]; then
  echo -e "\nLinting errors found 💥\n"
  cat ${Results}
  exit 1
else
  echo -e "\nEverything looks fine 🙂👍"
  exit 0
fi
