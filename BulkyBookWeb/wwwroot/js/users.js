var userDataTable;
var isLockUser;



$(document).ready(function () {
    userDataTable = $('#tblData').DataTable({
        ajax: {
            url: '/admin/user/getall',
            dataSrc: 'data'
        },
        columns: [
            { data: 'name', width: '25%' },
            { data: 'email', width: '30%' },
            {
                data: 'phoneNumber',
                defaultContent: 'No phone number',
                width: '15%'
            },
            {
                data: 'role',
                width: '10%',
                render: function (data) {
                    return '<span class="badge bg-secondary">'
                        + (data || '') + '</span>';
                }
            },
            {
                data: 'id',
                width: '20%',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    const lockButton = row.isLocked
                        ? `<button type="submit"
                                   class="btn btn-sm btn-success"
                                   style="width: 95px;">
                               <i class="bi bi-unlock-fill me-1"></i>Unlock
                           </button>`
                                            : `<button type="submit"
                                   class="btn btn-sm btn-danger"
                                   style="width: 95px;">
                               <i class="bi bi-lock-fill me-1"></i>Lock
                           </button>`;

                    return `
                        <div class="d-flex gap-2 justify-content-end">
                            <form method="post" action="/Admin/User/UserLock" class="m-0">
                                <input type="hidden" name="userId" value="${data}">
                                ${lockButton}
                            </form>

                            <a href="/Admin/User/Role?userId=${row.id}"
                               class="d-flex btn btn-sm btn-secondary text-white align-items-center">
                                <i class="bi bi-person-badge me-1"></i>Role
                            </a>
                            <a href="/Admin/User/ChangePassword?userId=${row.id}"
                               class="d-flex btn btn-sm btn-warning align-items-center">
                                <i class="bi bi-key me-1"></i>Password
                            </a>
                        </div>`;
                }
            }
        ]
    });
});