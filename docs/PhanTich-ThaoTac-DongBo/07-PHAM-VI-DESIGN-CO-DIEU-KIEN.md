# Pham Vi Design Co Dieu Kien - Nhat Tin Logistics

## Muc Tieu

Tai lieu nay chot pham vi co the bat dau design truoc dua tren phan hoi cua Nhat Tin trong [05-CAU-HOI-GUI-NHATTIN.md](05-CAU-HOI-GUI-NHATTIN.md). Cac muc chua duoc tra loi ro van giu trang thai can xac minh va khong duoc gia dinh thanh hanh vi san xuat.

Quyet dinh hien tai: **duyet Stage 2 Design co dieu kien** cho cac module da duoc xac nhan, chua duyet code sandbox san xuat day du cho nhung phan con phu thuoc IT Nhat Tin hoac live sandbox evidence.

## Cac Diem Da Xac Nhan

| Nhom | Ket luan dung cho design | Ghi chu |
| --- | --- | --- |
| API host | Sandbox `https://apisandbox.ntlogistics.vn`; Production `https://apiws.ntlogistics.vn`. | Cau hinh theo environment, khong hardcode trong code nghiep vu. |
| Portal web | Sandbox `https://bodev.ntlogistics.vn`; Production `https://khachhang.ntlogistics.vn`. | `partner_id` duoc tao/quan ly tren portal. |
| Auth header | Moi request authenticated gui header `Authorization` voi access token. | Thiet ke theo Bearer access token; van can live test login/refresh khi co credential. |
| Auth error | Token het han/sai/thieu co the tra `401`; client can refresh token roi retry mot lan. | TTL va refresh rotation con cho IT Nhat Tin. |
| Dia danh | Sau moc `01/07/2025`, tao van don dung dia danh moi. | CreateBill dung `s_province_code`, `s_ward_code`, `r_province_code`, `r_ward_code`. |
| CreateBill toi thieu | Co payload mau toi thieu de design DTO/validation va testcase happy path. | Van chua test duoc API that. |
| `partner_id` | La truong bat buoc, duoc tao tren Web portal. | Ap dung cho cac nghiep vu can partner context nhu tinh phi, cap nhat, in nhan. |
| Cancel/Revert | Huy cho trang thai `1` Chua thanh cong va `2` Cho lay hang; hoan tra cho `3`, `15`, `7`. | Update bill chua co ma tran trang thai ro. |
| Tracking | Tai lieu/phan hoi chi ho tro tra cuu 1 don moi lan. | Cach truyen `bill_code` query hay body con cho IT Nhat Tin. |
| Tracking timestamp | Timestamp khong co mot format/timezone co dinh. | Adapter phai parse tolerant va luu raw payload. |
| Webhook config | Cau hinh callback qua Web portal. | TruePos can co callback URL co the cau hinh theo moi truong. |
| Webhook method | Ben POS duoc chon method. | Khuyen nghi design receiver dung `POST` lam mac dinh. |
| Webhook signature | Khong co signature/header ky. | Can dedupe, audit raw payload, va co the them network control neu duoc IT xac nhan sau. |
| Webhook dedupe | POS tu xu ly trung lap. | Dedupe tam theo `bill_no`, `status_id`, `status_time`, `push_time` hoac hash payload. |
| Print endpoint | Goi `GET /v3/bill/print?do_code={bill_code}&partner_id={partner_id}`. | Query chinh thuc dung `do_code`; response type/content-type con cho IT Nhat Tin. |

## Pham Vi Duoc Bat Dau Truoc

| Module | Duoc phep design truoc | Gioi han bat buoc |
| --- | --- | --- |
| Environment config | Tach API host, portal host, partner_id theo environment. | Chua hardcode credential; chua ket luan print content-type. |
| Auth client | Token manager gui `Authorization`, refresh va retry mot lan khi gap `401`. | TTL, refresh rotation, body loi that phai cap nhat sau live test. |
| Location adapter | Uu tien dia danh moi cho CreateBill va CalcFee. | Quan he `*_code` voi `*_id` con phai kiem chung. |
| CreateBill DTO | Dung payload toi thieu da nhan de lap request model, validation co ban va happy path. | `return_ward_code`, bang ma loi, response that con mo. |
| Operation rules | Encode tam cancel/revert status da duoc xac nhan. | Update bill chi duoc design guarded, chua mo state transition rong. |
| Tracking polling | Thiet ke polling theo tung `bill_code`, khong batch nhieu don. | Request shape GET query/body con de sau. |
| Webhook receiver | Receiver mac dinh `POST`, khong signature, luu raw body/header, dedupe noi bo. | ACK status/body, retry policy, timeout con cho IT Nhat Tin. |
| Print request builder | Build URL voi `do_code` va `partner_id`. | Chua design storage/preview cuoi cung cho den khi biet response type. |

## Cac Diem Con Cho Cap Nhat

| Nhom | Con thieu | Tac dong |
| --- | --- | --- |
| Credential/live sandbox | Username, password, goi login va tao don that. | Chua co evidence de dong behavior thuc te. |
| Network | IP whitelist, VPN, callback domain. | Anh huong test webhook/API that. |
| Token lifecycle | TTL access/refresh, refresh rotation. | Anh huong lock refresh va re-login policy. |
| Master Data | Gia tri day du cho service, payment method, cargo type, status. | Anh huong seed, validation, state machine. |
| CreateBill conditional fields | `return_ward_code` va return address khi `is_return_org=0/1`. | Anh huong validation tao don. |
| Error catalog | Ma loi nghiep vu cho create/calc/update/cancel/revert. | Anh huong ErrorCodes va testcase fail path. |
| Tracking request | `bill_code` bang query string hay JSON body. | Anh huong client adapter. |
| Webhook ACK/retry | Status/body thanh cong, retry, timeout. | Anh huong receiver va replay. |
| Print response | PDF/binary, HTML, redirect, link, hay JSON envelope. | Anh huong luu file/preview/in nhan. |
| Asset links | Auth, TTL, quyen truy cap anh/chung tu. | Anh huong proxy va raw asset storage. |
| Rate limit | Quota endpoint/token/IP, tan suat polling. | Anh huong scheduler va backoff. |

## Nguyen Tac Khi Sang Stage 2

1. Moi gia dinh con mo phai nam trong config, adapter, hoac feature flag de doi duoc sau khi co phan hoi moi.
2. Luu raw request/response/webhook cho cac endpoint nghiep vu chinh.
3. Khong xoa marker `Can xac minh` neu chua co phan hoi ro hoac live evidence.
4. Cac test sandbox dau tien nen chi bao phu happy path da xac nhan va fail path auth `401` co refresh.
