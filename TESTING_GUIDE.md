# AllowanceDeduction Module - Testing Guide

## How to Test the Module

### 1. Navigate to the Module
1. Run the application
2. Login to the system
3. From the sidebar, under **FINANCE & PAYROLL** section, click **Allowances & Deductions**
4. You should see the AllowanceDeduction Index page with an empty table

### 2. Create a Company-Wide Allowance (e.g., House Rent)
1. Click the **"Add Allowance/Deduction"** button
2. A modal will open with the create form
3. Fill in the form:
   - **Type**: Select "Allowance"
   - **Name**: Enter "House Rent Allowance"
   - **Calculation Type**: Select "Fixed Amount"
   - **Fixed Amount**: Enter "5000"
   - **Effective From**: Select today's date
   - **Effective To**: Leave blank (ongoing)
   - **Scope**: Select "Company-Wide"
   - **Active**: Check the checkbox
4. Click **Save**
5. You should see a success message and the table will reload with the new record

### 3. Create an Employee-Specific Deduction (e.g., Loan Deduction)
1. Click **"Add Allowance/Deduction"** again
2. Fill in the form:
   - **Type**: Select "Deduction"
   - **Name**: Enter "Loan Deduction"
   - **Calculation Type**: Select "Fixed Amount"
   - **Fixed Amount**: Enter "2000"
   - **Effective From**: Select today's date
   - **Effective To**: Select a future date (e.g., 6 months from now)
   - **Scope**: Select "Employee-Specific"
   - **Employee**: Select an employee from the dropdown
   - **Active**: Check the checkbox
3. Click **Save**
4. Verify the new record appears in the table

### 4. Create a Percentage-Based Allowance (e.g., Medical)
1. Click **"Add Allowance/Deduction"** again
2. Fill in the form:
   - **Type**: Select "Allowance"
   - **Name**: Enter "Medical Allowance"
   - **Calculation Type**: Select "Percentage of Basic Salary"
   - **Percentage**: Enter "10"
   - **Effective From**: Select today's date
   - **Effective To**: Leave blank
   - **Scope**: Select "Company-Wide"
   - **Active**: Check the checkbox
3. Click **Save**

### 5. Edit an Existing Record
1. In the table, find a record you want to edit
2. Click the yellow **Edit** button (pencil icon)
3. The modal will open with the form pre-filled
4. Modify any fields (e.g., change the amount, update dates)
5. Click **Save**
6. Verify the changes are reflected in the table

### 6. Test Validation

#### Test Duplicate Names:
1. Try to create an allowance with the same name as an existing one (same type and scope)
2. You should see an error message preventing the duplicate

#### Test Invalid Percentage:
1. Create a new allowance with "Percentage" calculation type
2. Enter a percentage greater than 100 (e.g., 150)
3. You should see a validation error

#### Test Invalid Date Range:
1. Create a new allowance
2. Set "Effective To" date earlier than "Effective From"
3. You should see a validation error

#### Test Employee-Specific Without Employee:
1. Create a new allowance with "Employee-Specific" scope
2. Don't select an employee
3. Try to save - you should see a validation error

### 7. Test Delete Functionality
1. Find a record that is NOT linked to any payroll
2. Click the red **Delete** button (trash icon)
3. Confirm the deletion in the dialog
4. The record should be removed from the table

### 8. Verify Table Features

#### Search:
- Use the search box to filter records by name, type, etc.

#### Sorting:
- Click on any column header to sort by that column
- By default, table is sorted by "Effective From" in descending order

#### Pagination:
- If you have more than 10 records, test the pagination controls

### 9. Test Dynamic UI Behavior

#### Calculation Type Toggle:
1. Open the create form
2. Initially, you should see the dropdown but no amount fields (since nothing is selected)
3. Select "Fixed Amount" - verify that the Fixed Amount field appears below
4. Change to "Percentage of Basic Salary" - verify that the Fixed Amount field hides and the Percentage field appears
5. The field you're not using should automatically hide

**Note:** The amount fields are dynamically shown/hidden based on your Calculation Type selection. Only one field (Fixed Amount OR Percentage) will be visible at a time.

#### Scope Toggle:
1. Open the create form
2. By default, "Company-Wide" should be selected
3. Verify the Employee dropdown is hidden
4. Select "Employee-Specific" - verify the Employee dropdown appears with a searchable Select2 interface
5. Change back to "Company-Wide" - verify the Employee dropdown hides again

#### Select2 Functionality:
1. Open the create form
2. Select "Employee-Specific" scope
3. The Employee dropdown should appear
4. Click on the Employee dropdown
5. Type to search for an employee by name
6. Verify the search filters the employee list correctly
7. Select an employee from the filtered results


### 10. Verify Visual Elements

Check that the following visual elements display correctly:
- ? Type badges (Green for Allowance, Red for Deduction)
- ? Calculation Type badges (Blue for Fixed, Orange for Percentage)
- ? Scope badges (Primary for Company-Wide, Secondary for Employee-Specific)
- ? Status badges (Green for Active, Gray for Inactive)
- ? Currency formatting (? symbol for fixed amounts)
- ? Percentage symbol (% for percentage values)
- ? "N/A" for company-wide records in Employee column
- ? "Ongoing" for records without end date

### 11. Test Responsive Design
1. Resize your browser window to mobile size
2. Verify the table adapts responsively
3. Verify the modal form displays correctly on mobile
4. Test all functionality on mobile view

## Expected Behavior Summary

### Create:
- ? Form validates all required fields
- ? Prevents duplicate names
- ? Shows/hides fields based on selections
- ? Success message on save
- ? Table auto-reloads with new record

### Edit:
- ? Pre-fills form with existing data
- ? Applies same validation as create
- ? Updates record on save
- ? Table auto-refreshes

### Delete:
- ? Shows confirmation dialog
- ? Prevents deletion if linked to payroll
- ? Removes record on confirm
- ? Table auto-refreshes

### Validation Messages:
- ? Clear, user-friendly error messages
- ? Multiple errors shown together (not one-by-one)
- ? Field-specific guidance

## Sample Test Data

### Company-Wide Allowances:
1. House Rent Allowance - Fixed ?5,000
2. Medical Allowance - 10% of Basic Salary
3. Transport Allowance - Fixed ?3,000
4. Mobile Allowance - Fixed ?1,500

### Company-Wide Deductions:
1. Provident Fund - 5% of Basic Salary
2. Tax Deduction - 2% of Basic Salary

### Employee-Specific:
1. Loan Deduction (Employee A) - Fixed ?2,000
2. Special Allowance (Employee B) - Fixed ?4,000
3. Advance Deduction (Employee C) - Fixed ?1,000

## Troubleshooting

### If the menu item doesn't appear:
- Check that the link is in _Layout.cshtml (it should already be there)
- Check that you're logged in

### If the table doesn't load:
- Check browser console for JavaScript errors
- Verify the controller endpoint is accessible
- Check that the database connection is working

### If Select2 doesn't work:
- Check that Select2 CSS and JS are loaded
- Check browser console for errors
- Verify jQuery is loaded before Select2

### If form doesn't submit:
- Check browser console for JavaScript errors
- Verify CSRF token is present
- Check network tab for API response

### If validation errors don't show:
- Check that the repository validation is throwing ArgumentException
- Verify the controller is catching and returning proper error response
- Check SweetAlert is loaded correctly

## Integration Testing with Payroll (Future)

When payroll processing is implemented, test:
1. Company-wide allowances apply to all employees
2. Employee-specific allowances apply only to the designated employee
3. Fixed amounts are added/deducted as-is
4. Percentage amounts are calculated correctly based on basic salary
5. Inactive or expired allowances/deductions are not applied
6. Effective date ranges are respected
