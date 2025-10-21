#!/bin/bash

# Test script to verify GraphQL endpoint works

echo "Testing GraphQL Server..."
echo ""

echo "1. Testing basic connectivity:"
curl -X POST http://127.0.0.1:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __typename }"}' 2>/dev/null | jq .

echo ""
echo "2. Testing Query type exists:"
curl -X POST http://127.0.0.1:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __schema { queryType { name } } }"}' 2>/dev/null | jq .

echo ""
echo "3. Testing characters field:"
curl -X POST http://127.0.0.1:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __type(name: \"Query\") { fields { name } } }"}' 2>/dev/null | jq .

echo ""
echo "4. Testing characters field args:"
curl -X POST http://127.0.0.1:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __type(name: \"Query\") { fields(includeDeprecated: false) { name args { name type { kind name } } } } }"}' 2>/dev/null | jq .

echo ""
echo "5. Testing actual filtering query:"
curl -X POST http://127.0.0.1:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{"query":"{ characters(where: { name: { contains: \"Luke\" } }) { id name } }"}' 2>/dev/null | jq .
