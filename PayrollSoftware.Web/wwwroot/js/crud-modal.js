window.CrudModal = (function () {
    let $modal, $body, routes = {}, onSaved = null, titles = {};

    function init(options) {
        const {
            modalSelector,
            addBtnSelector,
            routes: rts,
            createTitle = 'Create',
            editTitle = 'Edit',
            onSaved: savedCb
        } = options || {};

        if (!modalSelector) throw new Error('CrudModal: modalSelector is required');
        if (!rts || (!rts.createGet && !rts.editGet)) throw new Error('CrudModal: routes.createGet/editGet are required');

        $modal = $(modalSelector);
        $body = $modal.find('.modal-body');
        routes = rts;
        onSaved = typeof savedCb === 'function' ? savedCb : null;
        titles = { createTitle, editTitle };

        if (addBtnSelector) {
            $(document).off('click.crud.add', addBtnSelector)
                .on('click.crud.add', addBtnSelector, openCreate);
        }

        // Intercept any form submission inside the modal
        $modal.off('submit.crud').on('submit.crud', 'form', function (e) {
            e.preventDefault();
            const $form = $(this);
            const url = $form.attr('action');
            const data = $form.serialize();
            CommonAjax.ajaxPost(url, data)
                .done(function (resp) {
                    $modal.modal('hide');
                    CommonAjax.toastSuccess(resp?.message || 'Saved successfully');
                    if (onSaved) onSaved(resp);
                })
                .fail(function (xhr) {
                    CommonAjax.toastError(xhr.responseJSON?.message || 'Request failed');
                });
        });

        return api;
    }

    function openCreate() {
        setModalTitle(titles.createTitle);
        const url = typeof routes.createGet === 'function' ? routes.createGet() : routes.createGet;
        CommonAjax.ajaxGet(url)
            .done(html => { $body.html(html); $modal.modal('show'); })
            .fail(() => CommonAjax.toastError('Unable to load form'));
    }

    function openEdit(id) {
        setModalTitle(titles.editTitle);
        const url = typeof routes.editGet === 'function' ? routes.editGet(id) : routes.editGet;
        CommonAjax.ajaxGet(url)
            .done(html => { $body.html(html); $modal.modal('show'); })
            .fail(() => CommonAjax.toastError('Unable to load form'));
    }

    function setModalTitle(text) {
        if (!$modal) return;
        const $title = $modal.find('.modal-title');
        if ($title.length) $title.text(text);
    }

    const api = { init, openCreate, openEdit };
    return api;
})();
