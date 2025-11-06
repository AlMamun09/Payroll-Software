# Quick Troubleshooting: AllowanceDeduction Form Fields

## Problem: "I can't see the Fixed Amount or Percentage field!"

### Solution:
The amount fields are **intentionally hidden by default**. They only appear when you select a **Calculation Type**.

**Step-by-step to see the fields:**

1. Click "Add Allowance/Deduction" button
2. Select a **Type** (Allowance or Deduction) - ? Do this first
3. Select a **Calculation Type**: - ? This is the key step!
   - Choose **"Fixed Amount"** ? The Fixed Amount input field will appear
   - Choose **"Percentage of Basic Salary"** ? The Percentage input field will appear
4. Now you can enter the amount in the visible field

### Why are they hidden?
This is **by design** to prevent confusion:
- If you choose "Fixed", you don't need the Percentage field
- If you choose "Percentage", you don't need the Fixed Amount field
- Only showing the relevant field makes the form cleaner and easier to use

## Visual Example

### Before Selecting Calculation Type:
```
Calculation Type: [-- Select Calculation Type --?]
(No amount field visible - this is normal!)
```

### After Selecting "Fixed Amount":
```
Calculation Type: [Fixed Amount ?]
Fixed Amount (?): [5000.00     ] ? This field now appears!
```

### After Selecting "Percentage":
```
Calculation Type: [Percentage of Basic Salary ?]
Percentage (%): [10.00       ] ? This field now appears!
```

## Problem: "The field appeared but now it's gone!"

### Solution:
When you **switch** between Fixed and Percentage, the fields swap:

**Example:**
1. Select "Fixed Amount" ? Fixed Amount field appears
2. Enter "5000" in Fixed Amount field
3. Change to "Percentage" ? Percentage field appears, Fixed Amount field hides
4. This is correct behavior!

The form ensures you can only use **one calculation method at a time**.

## Problem: "I can't select an employee!"

### Solution:
The Employee dropdown is also **intentionally hidden by default**. It only appears for Employee-Specific allowances/deductions.

**Step-by-step to see the Employee dropdown:**

1. Scroll down to the **Scope** section
2. You'll see two radio buttons:
   - ? Company-Wide (default - selected)
   - ? Employee-Specific
3. Click on **"Employee-Specific"** radio button
4. The Employee dropdown will now appear below
5. Click the dropdown to search and select an employee

### Why is it hidden?
- **Company-Wide** items apply to ALL employees, so there's no need to select a specific employee
- **Employee-Specific** items apply to ONE employee, so you need to select which one

## Problem: "Form validation error when saving"

### Common causes and solutions:

#### Error: "Calculation Type is required"
- **Cause:** You didn't select Fixed or Percentage
- **Solution:** Select a Calculation Type from the dropdown

#### Error: "Fixed Amount must be between..."
- **Cause:** You selected "Fixed" but didn't enter an amount, or entered an invalid amount
- **Solution:** 
  1. Make sure the Fixed Amount field is visible
  2. Enter a valid amount (greater than 0)

#### Error: "Percentage must be between 0% and 100%"
- **Cause:** You entered a percentage outside the valid range
- **Solution:** 
  1. Make sure the Percentage field is visible
  2. Enter a value between 0 and 100

#### Error: "Employee is required for employee-specific..."
- **Cause:** You selected "Employee-Specific" but didn't select an employee
- **Solution:** 
  1. Make sure the Employee dropdown is visible
  2. Click the dropdown and select an employee

## Testing the Dynamic Fields

### Quick Test 1: Toggle Calculation Type
```
1. Select "Fixed Amount" ? See Fixed Amount field
2. Enter "5000"
3. Select "Percentage" ? See Percentage field (Fixed Amount hides)
4. Enter "10"
5. Select "Fixed Amount" again ? Fixed Amount field reappears (Percentage hides)
6. ? Both fields should work correctly
```

### Quick Test 2: Toggle Scope
```
1. "Company-Wide" selected ? Employee dropdown hidden ?
2. Click "Employee-Specific" ? Employee dropdown appears ?
3. Click "Company-Wide" again ? Employee dropdown hides ?
```

## Browser Console Check

If fields are not appearing at all, check the browser console (F12):

1. Press **F12** to open Developer Tools
2. Click **Console** tab
3. Look for any red error messages
4. Common issues:
   - jQuery not loaded
   - Script errors preventing execution
   - Missing JavaScript files

## Form Field Reference

| Field Name | Always Visible? | When Visible? |
|------------|----------------|---------------|
| Type | ? Yes | Always |
| Name | ? Yes | Always |
| Calculation Type | ? Yes | Always |
| **Fixed Amount** | ? No | When "Fixed" selected |
| **Percentage** | ? No | When "Percentage" selected |
| Effective From | ? Yes | Always |
| Effective To | ? Yes | Always |
| Scope (radios) | ? Yes | Always |
| **Employee** | ? No | When "Employee-Specific" selected |
| Active (checkbox) | ? Yes | Always |

## Still Having Issues?

### Checklist:
- [ ] Did you select a Calculation Type? (This is required to see amount fields)
- [ ] Did you select the correct Scope? (Required to see Employee dropdown)
- [ ] Is JavaScript enabled in your browser?
- [ ] Are there any red errors in the browser console (F12)?
- [ ] Did you clear your browser cache?
- [ ] Try in a different browser or incognito/private window

### Where to Look:
1. **Browser Console** (F12 ? Console tab) - Look for JavaScript errors
2. **Network Tab** (F12 ? Network tab) - Make sure all scripts loaded
3. **Application Tab** (F12 ? Application ? Local Storage) - Clear if needed

## Expected Behavior Summary

? **CORRECT:**
- Amount fields appear AFTER selecting Calculation Type
- Only ONE amount field visible at a time (Fixed OR Percentage)
- Employee dropdown appears AFTER selecting Employee-Specific scope
- Fields toggle smoothly when switching selections

? **INCORRECT:**
- Both amount fields visible at the same time
- No amount field appears even after selecting Calculation Type
- Employee dropdown always hidden even when Employee-Specific selected
- Fields don't respond to dropdown changes

---

**Remember:** The dynamic field behavior is intentional and by design! The form shows only what you need based on your selections. This is a feature, not a bug! ??
