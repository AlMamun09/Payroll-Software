window.CommonAjax = (function () {
    function getCsrfHeader() {
        try {
            var token = window.__AntiForgeryToken;
            if (!token) {
                var el = document.querySelector('input[name="__RequestVerificationToken"]');
                token = el ? el.value : null;
            }
            return token ? { 'RequestVerificationToken': token } : {};
        } catch { return {}; }
    }

    function ajaxGet(url) {
        return $.ajax({ url, method: 'GET' });
    }

    function ajaxPost(url, data) {
        return $.ajax({
            url,
            method: 'POST',
            data,
            headers: getCsrfHeader()
        });
    }

    function confirm(message, title) {
        return Swal.fire({
            title: title || 'Are you sure?',
            text: message || '',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes',
            cancelButtonText: 'Cancel'
        });
    }

    function toastSuccess(message) {
        return Swal.fire({ title: message, icon: 'success', draggable: true });
    }

    function toastError(message) {
        return Swal.fire({ title: 'Error', text: message, icon: 'error', draggable: true });
    }

    return { ajaxGet, ajaxPost, confirm, toastSuccess, toastError };
})();