window.CrudTable = (function () {
  let table,
    cfg = {};

  function init(options) {
    const {
      tableSelector,
      listUrl,
      columns,
      idField = "id",
      containerSelector,
      actions = {},
      dataSrc = "data",
      dataTables = {},
      onReload,
      commonAjax, // <-- injected instead of global
    } = options || {};

    if (!tableSelector) throw new Error("CrudTable: tableSelector is required");
    if (!listUrl) throw new Error("CrudTable: listUrl is required");
    if (!Array.isArray(columns))
      throw new Error("CrudTable: columns array is required");

    cfg = {
      idField,
      actions,
      onReload,
      commonAjax, // <-- store as dependency
      containerSelector:
        containerSelector ||
        $(tableSelector).closest(".card, .container, body"),
    };

    const finalColumns = [...columns];
    if (actions && typeof actions.renderButtons === "function") {
      finalColumns.push({
        data: idField,
        orderable: false,
        className: "text-center",
        render: (id, type, row) => actions.renderButtons(id, row),
      });
    }

    table = $(tableSelector).DataTable({
      ajax: { url: listUrl, dataSrc },
      processing: true,
      responsive: true,
      columns: finalColumns,
      ...dataTables,
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

    if (cfg.actions?.editSelector && typeof cfg.actions.onEdit === "function") {
      $scope
        .off("click.crud.edit")
        .on("click.crud.edit", cfg.actions.editSelector, function () {
          const id = $(this).data("id");
          cfg.actions.onEdit(id);
        });
    }

    if (
      cfg.actions?.deleteSelector &&
      typeof cfg.actions.deleteUrl === "function"
    ) {
      $scope
        .off("click.crud.delete")
        .on("click.crud.delete", cfg.actions.deleteSelector, async () => {
          const id = $(event.target).data("id");
          const ajax = cfg.commonAjax;
          if (!ajax) throw new Error("CrudTable: commonAjax not provided");

          const r = await ajax.confirm("This will delete the item.");
          if (!r.isConfirmed) return;

          ajax
            .ajaxPost(cfg.actions.deleteUrl(id))
            .done(() => {
              ajax.toastSuccess("Deleted");
              reload();
            })
            .fail((x) =>
              ajax.toastError(x.responseJSON?.message || "Delete failed")
            );
        });
    }

    // Support for deactivate action
    if (
      cfg.actions?.deactivateSelector &&
      typeof cfg.actions.onDeactivate === "function"
    ) {
      $scope
        .off("click.crud.deactivate")
        .on(
          "click.crud.deactivate",
          cfg.actions.deactivateSelector,
          function () {
            const id = $(this).data("id");
            cfg.actions.onDeactivate(id);
          }
        );
    }

    // Support for activate action
    if (
      cfg.actions?.activateSelector &&
      typeof cfg.actions.onActivate === "function"
    ) {
      $scope
        .off("click.crud.activate")
        .on("click.crud.activate", cfg.actions.activateSelector, function () {
          const id = $(this).data("id");
          cfg.actions.onActivate(id);
        });
    }
  }

  const api = { init, reload, table: () => table };
  return api;
})();
