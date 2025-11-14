/* global Swal */
window.CommonAjax = (function () {
  // SweetAlert2 theme configuration
  const SWAL_CONFIG = {
    confirmButtonColor: "#3085d6",
    cancelButtonColor: "#d33",
    denyButtonColor: "#6c757d",
  };

  function getCsrfHeader() {
    try {
      var token = window.__AntiForgeryToken;
      if (!token) {
        var el = document.querySelector(
          'input[name="__RequestVerificationToken"]'
        );
        token = el ? el.value : null;
      }
      return token ? { RequestVerificationToken: token } : {};
    } catch {
      return {};
    }
  }

  function ajaxGet(url) {
    return $.ajax({ url, method: "GET" });
  }

  function ajaxPost(url, data) {
    return $.ajax({
      url,
      method: "POST",
      data,
      headers: getCsrfHeader(),
    });
  }

  function confirm(message, title) {
    return Swal.fire({
      title: title || "Are you sure?",
      text: message || "",
      icon: "warning",
      showCancelButton: true,
      confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
      cancelButtonColor: SWAL_CONFIG.cancelButtonColor,
      confirmButtonText: "Yes",
      cancelButtonText: "Cancel",
      reverseButtons: false,
    });
  }

  function toastSuccess(message, title) {
    return Swal.fire({
      title: title || "Success",
      text: message,
      icon: "success",
      confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
      timer: 2000,
      timerProgressBar: true,
    });
  }

  function toastError(message, title) {
    return Swal.fire({
      title: title || "Error",
      text: message,
      icon: "error",
      confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
    });
  }

  function toastWarning(message, title) {
    return Swal.fire({
      title: title || "Warning",
      text: message,
      icon: "warning",
      confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
    });
  }

  function toastInfo(message, title) {
    return Swal.fire({
      title: title || "Information",
      text: message,
      icon: "info",
      confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
    });
  }

  /**
   * Common AJAX form submission handler
   * @param {HTMLFormElement} form - The form element
   * @param {Object} options - Configuration options
   * @param {Function} options.onSuccess - Success callback (receives response data)
   * @param {Function} options.onError - Error callback (receives error message)
   * @param {Function} options.beforeSubmit - Called before submission (can return false to cancel)
   */
  function submitForm(form, options) {
    const {
      onSuccess,
      onError,
      beforeSubmit,
      successTitle = "Success",
      successMessage = "Operation completed successfully!",
      errorTitle = "Validation Error",
    } = options || {};

    form.addEventListener("submit", function (e) {
      e.preventDefault();

      // Call beforeSubmit hook if provided
      if (beforeSubmit && beforeSubmit() === false) {
        return;
      }

      const formData = new FormData(form);
      const token = form.querySelector(
        'input[name="__RequestVerificationToken"]'
      )?.value;

      fetch(form.action, {
        method: "POST",
        body: formData,
        headers: token ? { RequestVerificationToken: token } : {},
      })
        .then(async (response) => {
          const data = await response.json();
          if (!response.ok) {
            return Promise.reject(data);
          }
          return data;
        })
        .then((data) => {
          if (data.success) {
            Swal.fire({
              icon: "success",
              title: successTitle,
              text: data.message || successMessage,
              confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
              timer: 2000,
              timerProgressBar: true,
            }).then(() => {
              if (onSuccess) {
                onSuccess(data);
              } else {
                window.location.reload();
              }
            });
          } else {
            const errorMsg = data.message || "Operation failed";
            Swal.fire({
              icon: "error",
              title: "Error",
              text: errorMsg,
              confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
            });
            if (onError) onError(errorMsg);
          }
        })
        .catch((err) => {
          let msg = "Operation failed";

          if (err && typeof err === "object") {
            if (err.message) msg = err.message;
            else if (err.error) msg = err.error;
            else if (err.title) msg = err.title;
          } else if (typeof err === "string") {
            msg = err;
          }

          Swal.fire({
            icon: "error",
            title: errorTitle,
            html: msg.replace(/\n/g, "<br>"),
            width: "600px",
            confirmButtonColor: SWAL_CONFIG.confirmButtonColor,
          });

          if (onError) onError(msg);
        });
    });
  }

  /**
   * Initialize Select2 dropdowns with common settings
   * @param {string|jQuery} selector - The selector for Select2 elements
   * @param {Object} options - Additional Select2 options
   */
  function initSelect2(selector, options) {
    const defaultOptions = {
      theme: "bootstrap4",
      width: "100%",
      allowClear: true,
    };

    $(selector).each(function () {
      const $el = $(this);
      const placeholder = $el.data("placeholder") || "Select...";
      const dropdownParent =
        $el.closest(".modal").length > 0 ? $el.closest(".modal") : undefined;

      $el.select2(
        $.extend(
          {},
          defaultOptions,
          {
            placeholder: placeholder,
            dropdownParent: dropdownParent,
          },
          options || {}
        )
      );
    });
  }

  /**
   * Handle AJAX delete with confirmation
   * @param {string} url - Delete endpoint URL
   * @param {Object} options - Configuration options
   */
  function deleteRecord(url, options) {
    const {
      confirmMessage = "Are you sure you want to delete this record?",
      confirmTitle = "Delete Confirmation",
      successMessage = "Deleted successfully!",
      onSuccess,
      onError,
    } = options || {};

    return confirm(confirmMessage, confirmTitle).then((result) => {
      if (result.isConfirmed) {
        return $.ajax({
          url: url,
          type: "POST",
          headers: getCsrfHeader(),
        })
          .done(function (response) {
            toastSuccess(response?.message || successMessage);
            if (onSuccess) onSuccess(response);
          })
          .fail(function (xhr) {
            const errorMsg =
              xhr.responseJSON?.message || xhr.responseText || "Delete failed";
            toastError(errorMsg);
            if (onError) onError(errorMsg);
          });
      }
      return Promise.reject("Cancelled");
    });
  }

  /**
   * Initialize DataTable with common settings
   * @param {string|jQuery} selector - Table selector
   * @param {Object} options - DataTable options
   */
  function initDataTable(selector, options) {
    const defaultOptions = {
      processing: true,
      serverSide: false,
      language: {
        emptyTable: "No records found",
        zeroRecords: "No matching records found",
      },
    };

    return $(selector).DataTable($.extend({}, defaultOptions, options || {}));
  }

  /**
   * Calculate business days between two dates (Sunday-Thursday)
   * @param {Date|string} startDate - Start date
   * @param {Date|string} endDate - End date
   * @returns {number} Number of business days
   */
  function calculateBusinessDays(startDate, endDate) {
    const start = new Date(startDate);
    const end = new Date(endDate);

    start.setHours(0, 0, 0, 0);
    end.setHours(0, 0, 0, 0);

    if (end < start) return -1;

    let days = 0;
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      const dow = d.getDay(); // 0=Sun, 1=Mon, ..., 6=Sat
      // Business days: Sunday(0) through Thursday(4)
      if (dow !== 5 && dow !== 6) days++; // Exclude Friday(5) and Saturday(6)
    }
    return days;
  }

  /**
   * Calculate age from date of birth
   * @param {Date|string} dob - Date of birth
   * @param {Date|string} onDate - Date to calculate age on (default: today)
   * @returns {number} Age in years
   */
  function calculateAge(dob, onDate) {
    const birthDate = new Date(dob);
    const currentDate = onDate ? new Date(onDate) : new Date();

    let age = currentDate.getFullYear() - birthDate.getFullYear();
    const monthDiff = currentDate.getMonth() - birthDate.getMonth();

    if (
      monthDiff < 0 ||
      (monthDiff === 0 && currentDate.getDate() < birthDate.getDate())
    ) {
      age--;
    }

    return age;
  }

  /**
   * Format date to locale string
   * @param {Date|string} date - Date to format
   * @param {string} locale - Locale string (default: user's locale)
   * @returns {string} Formatted date string
   */
  function formatDate(date, locale) {
    if (!date) return "";
    const d = new Date(date);
    return d.toLocaleDateString(locale);
  }

  /**
   * Debounce function to limit function calls
   * @param {Function} func - Function to debounce
   * @param {number} wait - Wait time in milliseconds
   * @returns {Function} Debounced function
   */
  function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        clearTimeout(timeout);
        func(...args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  }

  /**
   * Load lookup values for dynamic dropdowns
   * @param {string} lookupType - Type of lookup (e.g., "Gender", "LeaveType")
   * @param {string} targetSelector - Selector for target dropdown
   * @param {string} selectedValue - Optional selected value
   * @returns {Promise} jQuery AJAX promise
   */
  function loadLookup(lookupType, targetSelector, selectedValue) {
    return $.ajax({
      url: "/Lookup/GetLookupsByType",
      type: "GET",
      data: { lookupType: lookupType },
      success: function (response) {
        if (response.success && response.data) {
          const $select = $(targetSelector);
          $select.empty();
          $select.append('<option value="">-- select --</option>');

          response.data.forEach(function (item) {
            const option = new Option(
              item.lookupValue,
              item.lookupValue,
              false,
              item.lookupValue === selectedValue
            );
            $select.append(option);
          });

          // Refresh Select2 if applicable
          if ($select.hasClass("select2-hidden-accessible")) {
            $select.trigger("change.select2");
          }
        }
      },
      error: function (xhr) {
        console.error("Error loading lookup:", lookupType, xhr.responseText);
      },
    });
  }

  return {
    // AJAX functions
    ajaxGet,
    ajaxPost,

    // SweetAlert wrappers
    confirm,
    toastSuccess,
    toastError,
    toastWarning,
    toastInfo,

    // Form handling
    submitForm,

    // UI components
    initSelect2,
    initDataTable,

    // CRUD operations
    deleteRecord,

    // Utility functions
    calculateBusinessDays,
    calculateAge,
    formatDate,
    debounce,
    loadLookup,

    // CSRF helper
    getCsrfHeader,

    // Config
    SWAL_CONFIG,
  };
})();
