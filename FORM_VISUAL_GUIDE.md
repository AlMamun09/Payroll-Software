# AllowanceDeduction Form - Visual Guide

## How the Create/Edit Form Works

### Form Layout Overview

```
???????????????????????????????????????????????????????????????????
?  Allowance/Deduction Form         ?
???????????????????????????????????????????????????????????????????
?      ?
?  [Type ?]             [Name            ]  ?
?   Allowance/Deduction         e.g., House Rent    ?
?       ?
?  [Calculation Type ?]       [Amount Field (Dynamic)]          ?
?   Fixed/Percentage  Shows based on selection ?         ?
?          ?
?  [Effective From]      [Effective To]          ?
?   [2025-01-15]  [2025-12-31] (optional)           ?
? ?
?  Scope:       ?
?  ? Company-Wide (all employees)      ?
?  ? Employee-Specific       ?
?    ?
?  [Employee Dropdown (Dynamic)]?
?   Shows only when Employee-Specific is selected                 ?
? ?
?  ? Active   ?
?         ?
?  ?? Information Panel ?
?     ?
?      [?? Save]?
???????????????????????????????????????????????????????????????????
```

## Dynamic Field Behavior

### Scenario 1: Creating a Fixed Amount Allowance

**Step-by-step:**

1. **Initial State** (when modal opens):
   ```
   Type: [-- Select Type --?]
   Name: [            ]
   Calculation Type: [-- Select Calculation Type --?]
   (No amount field visible yet)
   ```

2. **After selecting "Allowance" and "Fixed Amount":**
   ```
   Type: [Allowance ?]
   Name: [House Rent Allowance]
   Calculation Type: [Fixed Amount ?]
   Fixed Amount (?): [5000.00     ] ? This field appears!
   ```
   The Percentage field remains hidden.

### Scenario 2: Creating a Percentage-Based Allowance

**Step-by-step:**

1. **After selecting "Allowance" and "Percentage":**
   ```
   Type: [Allowance ?]
   Name: [Medical Allowance]
   Calculation Type: [Percentage of Basic Salary ?]
   Percentage (%): [10.00       ] ? This field appears!
   Enter percentage value (0-100%)
   ```
   The Fixed Amount field remains hidden.

### Scenario 3: Company-Wide vs Employee-Specific

#### Company-Wide (Default):
```
Scope:
? Company-Wide (all employees)
? Employee-Specific

(Employee dropdown is hidden)
```

#### Employee-Specific:
```
Scope:
? Company-Wide (all employees)
? Employee-Specific

Employee: [?? Search employee...  ?] ? Searchable dropdown appears!
```

## Field Visibility Rules

| User Action | Fixed Amount Field | Percentage Field | Employee Dropdown |
|-------------|-------------------|------------------|-------------------|
| Form opens | ? Hidden | ? Hidden | ? Hidden |
| Select "Fixed" | ? Visible | ? Hidden | Depends on scope |
| Select "Percentage" | ? Hidden | ? Visible | Depends on scope |
| Select "Company-Wide" | Depends on calc type | Depends on calc type | ? Hidden |
| Select "Employee-Specific" | Depends on calc type | Depends on calc type | ? Visible |

## Example: Complete Form Filled

### Example 1: Company-Wide Fixed Allowance
```
???????????????????????????????????????????????????????????????????
?  Create Allowance/Deduction       ?
???????????????????????????????????????????????????????????????????
?               ?
?  Type: [Allowance ?]    Name: [House Rent Allowance]  ?
?          ?
?  Calculation Type: [Fixed ?]      Fixed Amount (?): [5000.00]   ?
?           Enter fixed amount in Taka    ?
?        ?
?Effective From: [2025-01-01]     Effective To: [ ]   ?
?     Leave blank for ongoing       ?
?        ?
?  Scope:           ?
?  ? Company-Wide (all employees)   ?? Selected      ?
?  ? Employee-Specific       ?
?          ?
?  ? Active              ?
?        ?
?  ?? This will apply ?5,000 to all employees             ?
?              ?
?     [?? Save]   ?
???????????????????????????????????????????????????????????????????
```

### Example 2: Employee-Specific Percentage Deduction
```
???????????????????????????????????????????????????????????????????
?  Create Allowance/Deduction   ?
???????????????????????????????????????????????????????????????????
?   ?
?  Type: [Deduction ?]        Name: [Special Tax Deduction] ?
?          ?
?  Calculation Type: [Percentage ?]  Percentage (%): [5.00]       ?
?        Enter percentage (0-100%)    ?
?       ?
?  Effective From: [2025-01-01]   Effective To: [2025-06-30] ?
?      ?
?  Scope:        ?
?  ? Company-Wide (all employees)     ?
?  ? Employee-Specific     ?? Selected      ?
?               ?
?  Employee: [?? John Doe ?]        ?? Searchable dropdown!      ?
?               ?
?  ? Active         ?
?         ?
?  ?? This will deduct 5% of basic salary for John Doe           ?
?   ?
?[?? Save]              ?
???????????????????????????????????????????????????????????????????
```

## Testing Checklist

### ? Dynamic Field Display
- [ ] Open form ? No amount fields visible
- [ ] Select "Fixed" ? Fixed Amount field appears
- [ ] Select "Percentage" ? Percentage field appears
- [ ] Switch between Fixed and Percentage ? Correct field shows/hides
- [ ] Select "Company-Wide" ? Employee dropdown hidden
- [ ] Select "Employee-Specific" ? Employee dropdown appears
- [ ] Switch scope ? Employee dropdown shows/hides correctly

### ? Field Values
- [ ] Enter Fixed Amount ? Value saves correctly
- [ ] Enter Percentage ? Value saves correctly
- [ ] Switch calculation type ? Previous value is cleared
- [ ] Select Employee ? Selection saves correctly
- [ ] Clear employee when switching to Company-Wide

### ? Edit Mode
- [ ] Open existing Fixed record ? Fixed Amount field visible with value
- [ ] Open existing Percentage record ? Percentage field visible with value
- [ ] Open Company-Wide record ? Employee dropdown hidden
- [ ] Open Employee-Specific record ? Employee dropdown visible with selection

## Common Issues & Solutions

### Issue: Amount fields not showing
**Solution:** Make sure you select a Calculation Type first. The amount fields only appear after selecting "Fixed" or "Percentage".

### Issue: Can't select employee
**Solution:** First select "Employee-Specific" scope. The employee dropdown only appears when this option is selected.

### Issue: Form validation error on submit
**Possible causes:**
1. No amount entered (make sure the correct field is visible and has a value)
2. No employee selected for Employee-Specific scope
3. Invalid percentage (must be 0-100)
4. Invalid date range (Effective To must be after Effective From)

### Issue: Field has value but doesn't show
**Solution:** This shouldn't happen with the updated code. If it does:
1. Check browser console for JavaScript errors
2. Verify jQuery and all scripts are loaded
3. Clear browser cache and reload

## JavaScript Logic Summary

```javascript
// When Calculation Type changes:
if (type === "Fixed") {
    Show: Fixed Amount field
    Hide: Percentage field
    Set Percentage to 0
} else if (type === "Percentage") {
    Show: Percentage field
    Hide: Fixed Amount field
Set Fixed Amount to 0
} else {
    Hide: Both fields
}

// When Scope changes:
if (scope === "Company-Wide") {
    Hide: Employee dropdown
    Clear: Employee selection
} else {
    Show: Employee dropdown
    Require: Employee selection
}
```

## Tips for Smooth Testing

1. **Always select Calculation Type second** - After selecting Type, immediately select Calculation Type to see the amount field
2. **Use Tab key** - Helps verify field visibility and navigation
3. **Test switching** - Change selections back and forth to ensure hiding/showing works
4. **Check console** - Open browser DevTools (F12) to see any JavaScript errors
5. **Test in Edit mode** - Make sure fields populate correctly when editing existing records

## Expected Behavior Summary

| Calculation Type | Visible Field | Hidden Field | Value Saved |
|------------------|---------------|--------------|-------------|
| Fixed | Fixed Amount | Percentage | FixedAmount property |
| Percentage | Percentage | Fixed Amount | Percentage property |

| Scope | Employee Dropdown | EmployeeId Value |
|-------|-------------------|------------------|
| Company-Wide | Hidden | null |
| Employee-Specific | Visible | Selected Employee GUID |

---

**Remember:** The form intelligently shows only the fields you need based on your selections. This prevents confusion and ensures data integrity!
