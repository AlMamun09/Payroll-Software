window.CrudTable = (function () {
    let table, cfg = {};

    function init(options) {
        const {
            tableSelector,
            listUrl,
            columns,
            idField = 'id',
            containerSelector, // optional for delegated events scoping
            actions = {}, // { editSelector, deleteSelector, renderButtons(id,row), onEdit(id), deleteUrl(id) }
            dataSrc = 'data',
            dataTables = {}, // pass-through DT options
            onReload
        } = options || {};

        if (!tableSelector) throw new Error('CrudTable: tableSelector is required');
        if (!listUrl) throw new Error('CrudTable: listUrl is required');
        if (!Array.isArray(columns)) throw new Error('CrudTable: columns array is required');

        cfg = { idField, actions, onReload, containerSelector: containerSelector || $(tableSelector).closest('.card, .container, body') };

        const finalColumns = [...columns];
        if (actions && typeof actions.renderButtons === 'function') {
            finalColumns.push({
                data: idField,
                orderable: false,
                className: 'text-center',
                render: (id, type, row) => actions.renderButtons(id, row)
            });
        }

        table = $(tableSelector).DataTable({
            ajax: { url: listUrl, dataSrc },
            processing: true,
            responsive: true,
            columns: finalColumns,
            ...dataTables
        });

        bindActions();
        return api;
    }

    function reload() {
        if (table) table.ajax.reload(null, false);
        if (cfg.onReload) cfg.onReload();
    }

    function bindActions() {
        const $scope = $(cfg.containerSelector);
        if (cfg.actions?.editSelector && typeof cfg.actions.onEdit === 'function') {
            $scope.off('click.crud.edit').on('click.crud.edit', cfg.actions.editSelector, function () {
                const id = $(this).data('id');
                cfg.actions.onEdit(id);
            });
        }
        if (cfg.actions?.deleteSelector && typeof cfg.actions.deleteUrl === 'function') {
            $scope.off('click.crud.delete').on('click.crud.delete', cfg.actions.deleteSelector, async function () {
                const id = $(this).data('id');
                const r = await CommonAjax.confirm('This will delete the item.');
                if (r.isConfirmed) {
                    CommonAjax.ajaxPost(cfg.actions.deleteUrl(id))
                        .done(() => { CommonAjax.toastSuccess('Deleted'); reload(); })
                        .fail(x => CommonAjax.toastError(x.responseJSON?.message || 'Delete failed'));
                }
            });
        }
    }

    const api = { init, reload, table: () => table };
    return api;
})();
