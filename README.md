# ARM Template Parser

## What is it?

This is a tool that leverages Microsoft libraries to parse ARM templates offline. It fills out the parameters and interprets any statements to produce an array of resources in json format. The use case for the tool is local parsing of templates as part of automation. Specifically for copying policy assignments from upstream modules to modules written in other IaC languages, such as Terraform and Bicep.

## How to use it?


