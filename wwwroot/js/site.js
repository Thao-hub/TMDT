// Các tương tác lọc bộ sưu tập được khai báo trong trang chủ.
document.querySelectorAll('[data-hien-mat-khau]').forEach(nut => {
    nut.addEventListener('click', () => {
        const truong = nut.parentElement.querySelector('input');
        const dangHien = truong.type === 'password';
        truong.type = dangHien ? 'text' : 'password';
        nut.classList.toggle('dang-hien', dangHien);
        nut.setAttribute('aria-label', dangHien ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
        nut.title = dangHien ? 'Ẩn mật khẩu' : 'Hiện mật khẩu';
    });
});

document.querySelectorAll('[data-menu-tai-khoan]').forEach(cum => {
    const nut = cum.querySelector('.nut-tai-khoan-icon');
    const menu = cum.querySelector('.menu-tai-khoan');

    nut.addEventListener('click', () => {
        const dangMo = !menu.hidden;
        menu.hidden = dangMo;
        nut.setAttribute('aria-expanded', String(!dangMo));
    });

    document.addEventListener('click', suKien => {
        if (!cum.contains(suKien.target)) {
            menu.hidden = true;
            nut.setAttribute('aria-expanded', 'false');
        }
    });

    document.addEventListener('keydown', suKien => {
        if (suKien.key === 'Escape' && !menu.hidden) {
            menu.hidden = true;
            nut.setAttribute('aria-expanded', 'false');
            nut.focus();
        }
    });
});

document.querySelector('.nut-len-dau')?.addEventListener('click', () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
});

const hoaDonDat = document.querySelector('#hoaDonDat');
const thanhToanQR = document.querySelector('#thanhToanQR');

if (hoaDonDat) {
    const ngayBatDau = document.querySelector('#NgayBatDauThue');
    const ngayKetThuc = document.querySelector('#NgayKetThucThue');
    const giaThueMoiNgay = Number(hoaDonDat.dataset.giaThue);
    const tienDatCoc = Number(hoaDonDat.dataset.tienCoc);
    const dinhDangTien = new Intl.NumberFormat('vi-VN', {
        maximumFractionDigits: 0
    });
    const hienThiTien = giaTri => `${dinhDangTien.format(giaTri)} ₫`;

    const capNhatHoaDon = () => {
        const anhQR = document.querySelector('#anhQRDatThue');
        const soTienQR = document.querySelector('#soTienQRDatThue');
        if (!ngayBatDau.value || !ngayKetThuc.value) {
            document.querySelector('#soNgayTinh').textContent = '—';
            document.querySelector('#tongTienThue').textContent = '—';
            document.querySelector('#tongTienThueBill').textContent = '—';
            document.querySelector('#tongDuKien').textContent = '—';
            if (anhQR && soTienQR) {
                anhQR.hidden = true;
                soTienQR.textContent = 'Chọn ngày hợp lệ';
            }
            return;
        }

        const [namBD, thangBD, ngayBD] = ngayBatDau.value.split('-').map(Number);
        const [namKT, thangKT, ngayKT] = ngayKetThuc.value.split('-').map(Number);
        const mocBD = Date.UTC(namBD, thangBD - 1, ngayBD);
        const mocKT = Date.UTC(namKT, thangKT - 1, ngayKT);
        const soNgay = Math.floor((mocKT - mocBD) / 86400000) + 1;

        if (soNgay < 1) {
            document.querySelector('#soNgayTinh').textContent = 'Ngày trả phải sau ngày bắt đầu';
            document.querySelector('#tongTienThue').textContent = '—';
            document.querySelector('#tongTienThueBill').textContent = '—';
            document.querySelector('#tienDatCoc').textContent = hienThiTien(tienDatCoc);
            document.querySelector('#tongDuKien').textContent = '—';
            if (anhQR && soTienQR) {
                anhQR.hidden = true;
                soTienQR.textContent = 'Chọn ngày hợp lệ';
            }
            return;
        }

        const tongTienThue = giaThueMoiNgay * soNgay;
        document.querySelector('#soNgayTinh').textContent = `${soNgay} ngày`;
        document.querySelector('#tongTienThue').textContent = hienThiTien(tongTienThue);
        document.querySelector('#tongTienThueBill').textContent = hienThiTien(tongTienThue);
        document.querySelector('#tienDatCoc').textContent = hienThiTien(tienDatCoc);
        document.querySelector('#tongDuKien').textContent = hienThiTien(tongTienThue + tienDatCoc);
        if (thanhToanQR) {
            const ngayBatDauMa = ngayBatDau.value.replaceAll('-', '');
            const ngayKetThucMa = ngayKetThuc.value.replaceAll('-', '');
              const noiDung = `COC VAY M${thanhToanQR.dataset.dressId} ${ngayBatDauMa} ${ngayKetThucMa}`;
            const duongDanQR = new URL(`https://img.vietqr.io/image/${encodeURIComponent(thanhToanQR.dataset.bankId)}-${encodeURIComponent(thanhToanQR.dataset.accountNumber)}-compact2.svg`);
            duongDanQR.searchParams.set('amount', String(Math.round(tienDatCoc)));
            duongDanQR.searchParams.set('addInfo', noiDung);
            duongDanQR.searchParams.set('accountName', thanhToanQR.dataset.accountName);
            anhQR.src = duongDanQR.toString();
            anhQR.hidden = false;
            soTienQR.textContent = hienThiTien(tienDatCoc);
            document.querySelector('#noiDungQRDatThue').textContent = noiDung;
        }
    };

    ngayBatDau.addEventListener('change', capNhatHoaDon);
    ngayKetThuc.addEventListener('change', capNhatHoaDon);
    capNhatHoaDon();
}

const tepAnhChuyenKhoan = document.querySelector('#AnhChuyenKhoan');
const xemTruocAnhChuyenKhoan = document.querySelector('#xemTruocAnhChuyenKhoan');

if (tepAnhChuyenKhoan && xemTruocAnhChuyenKhoan) {
    let duongDanXemTruoc;
    tepAnhChuyenKhoan.addEventListener('change', () => {
        if (duongDanXemTruoc) URL.revokeObjectURL(duongDanXemTruoc);
        const tep = tepAnhChuyenKhoan.files?.[0];
        if (!tep) {
            xemTruocAnhChuyenKhoan.hidden = true;
            xemTruocAnhChuyenKhoan.removeAttribute('src');
            return;
        }

        duongDanXemTruoc = URL.createObjectURL(tep);
        xemTruocAnhChuyenKhoan.src = duongDanXemTruoc;
        xemTruocAnhChuyenKhoan.hidden = false;
    });
}

const lichBaoTriVay = document.querySelector('#lichBaoTriVay');

if (lichBaoTriVay) {
    const ngayBatDau = document.querySelector('#NgayBatDauThue');
    const ngayKetThuc = document.querySelector('#NgayKetThucThue');
    const canhBaoLich = document.querySelector('#canhBaoLich');
    const cacKhoangBan = [...lichBaoTriVay.querySelectorAll('[data-ngay-bat-dau]')];

    const kiemTraLich = () => {
        canhBaoLich.hidden = true;
        canhBaoLich.textContent = '';
        if (!ngayBatDau.value || !ngayKetThuc.value || ngayKetThuc.value < ngayBatDau.value) return;

        const biTrungLich = cacKhoangBan.some(khoang =>
            ngayBatDau.value <= khoang.dataset.ngayKetThucChan
            && ngayKetThuc.value >= khoang.dataset.ngayBatDau);

        if (biTrungLich) {
            canhBaoLich.textContent = 'Khoảng ngày này trùng lịch thuê hoặc ngày tiệm xử lý váy. Vui lòng chọn ngày khác.';
            canhBaoLich.hidden = false;
        }
    };

    ngayBatDau.addEventListener('change', kiemTraLich);
    ngayKetThuc.addEventListener('change', kiemTraLich);
    kiemTraLich();
}
