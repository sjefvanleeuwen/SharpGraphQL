# Troubleshooting

## Common Issues

### "Table 'X' not found in schema"
- **Cause**: Table names in JSON don't match GraphQL type names
- **Solution**: Use exact type names from schema

### "Field 'x' is required but not provided"
- **Cause**: Missing required fields (marked with `!`) in JSON data
- **Solution**: Add missing fields or make them optional in schema

### Relationships not resolving
- **Cause**: Incorrect foreign key field naming
- **Solution**: Use `<fieldName>Id` for single, `<fieldName>Ids` for multiple

## Error Messages

**Schema Validation Errors:**
```
ERROR: Type 'Post' references unknown type 'User'
FIX: Ensure all referenced types are defined in schema
```

**Data Validation Errors:**
```
ERROR: Field 'email' is required but not provided for record 'user1'
FIX: Add "email" field to JSON or make it optional with 'email: String'
```

**Relationship Errors:**
```
ERROR: Foreign key 'authorId' references non-existent record 'user999'
FIX: Ensure referenced records exist before creating relationships
```

## Performance Issues

### Slow Queries
1. **Add indexes** on frequently queried columns
2. **Check cache hit ratios** - increase cache size if low
3. **Profile queries** - identify bottlenecks
4. **Consider denormalization** for read-heavy workloads

### High Memory Usage
1. **Reduce MemTable capacity** if not write-heavy
2. **Reduce page cache size** if memory constrained
3. **Monitor index sizes** - consider selective indexing

### Storage Issues
1. **Check disk space** - database files can grow large
2. **Monitor page count** - indicates storage efficiency
3. **Consider compression** for cold data
