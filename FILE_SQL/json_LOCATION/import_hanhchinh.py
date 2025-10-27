import json
import pyodbc

conn = pyodbc.connect(
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=HOANGHYPC\\SQLEXPRESS;"
    "DATABASE=DBRealEstate;"
    "Trusted_Connection=yes;"
)
cur = conn.cursor()

path_province = r"D:\BaiTap\Project1\json_vitri\tinh_tp.json"
path_district = r"D:\BaiTap\Project1\json_vitri\quan_huyen.json"
path_commune = r"D:\BaiTap\Project1\json_vitri\xa_phuong.json"

# ======== PROVINCE =========
with open(path_province, 'r', encoding='utf-8') as f:
    provinces = json.load(f)

prov_code_to_id = {}
for code, info in provinces.items():
    code = code.zfill(2)
    name = info.get('name')
    cur.execute("SELECT ProvinceID FROM Province WHERE ProvinceName = ?", name)
    row = cur.fetchone()
    if row:
        pid = row[0]
    else:
        cur.execute("INSERT INTO Province (ProvinceName) VALUES (?)", name)
        cur.execute("SELECT SCOPE_IDENTITY()")
        pid = int(cur.fetchone()[0])
        conn.commit()
    prov_code_to_id[code] = pid

print(f"✅ {len(prov_code_to_id)} tỉnh/thành")

# ======== DISTRICT =========
with open(path_district, 'r', encoding='utf-8') as f:
    districts = json.load(f)

dist_code_to_id = {}
for code, info in districts.items():
    code = code.zfill(3)  # huyện 3 ký tự
    name = info.get('name')
    parent_prov_code = (info.get('parent_code') or "").zfill(2)
    province_id = prov_code_to_id.get(parent_prov_code)

    if not province_id:
        print(f"⚠️ Không tìm thấy tỉnh cho huyện {name} (mã={code})")
        continue

    cur.execute("SELECT DistrictID FROM District WHERE DistrictName = ? AND ProvinceID = ?", name, province_id)
    row = cur.fetchone()
    if row:
        did = row[0]
    else:
        cur.execute("INSERT INTO District (DistrictName, ProvinceID) VALUES (?, ?)", name, province_id)
        cur.execute("SELECT SCOPE_IDENTITY()")
        did = int(cur.fetchone()[0])
        conn.commit()
    dist_code_to_id[code] = did

print(f"✅ {len(dist_code_to_id)} quận/huyện")

# ======== COMMUNE =========
with open(path_commune, 'r', encoding='utf-8') as f:
    communes = json.load(f)

count_inserted = 0
for code, info in communes.items():
    cname = info.get('name')
    parent_dist_code = (info.get('parent_code') or "").zfill(3)  # huyện 3 ký tự
    district_id = dist_code_to_id.get(parent_dist_code)

    if not district_id:
        print(f"❌ Không tìm thấy huyện {parent_dist_code} cho xã/phường {cname}")
        continue

    cur.execute("SELECT CommuneID FROM CommuneWard WHERE CommuneName = ? AND DistrictID = ?", cname, district_id)
    if cur.fetchone():
        continue

    cur.execute(
        "INSERT INTO CommuneWard (CommuneName, DistrictID) VALUES (?, ?)",
        (cname, district_id)
    )
    count_inserted += 1
    if count_inserted % 1000 == 0:
        conn.commit()

conn.commit()
cur.close()
conn.close()
print(f"✅ Đã chèn {count_inserted} xã/phường thành công!")
