# Quy tắc Git Branching & Pull Request

> **Mục đích**: Cung cấp mô hình branching nhẹ, an toàn, phù hợp cho team và đảm bảo kiểm soát chất lượng code.  
> **Last Updated**: 2026-04-06

---

## §1 — Mô hình Branching

### 1.1 Trunk-based Development

Sử dụng **feature branches ngắn hạn** tách từ `main`. Giữ branch tập trung và tồn tại ngắn (giờ → ngày, không phải tuần).

### 1.2 Quy tắc đặt tên Branch

```
{type}/{scope}-{short-desc}
```

| Type | Mục đích | Ví dụ |
|------|----------|-------|
| `feature` | Tính năng mới | `feature/asset-bulk-upload` |
| `fix` | Sửa bug | `fix/login-nullref` |
| `refactor` | Cải thiện code | `refactor/asset-service-cleanup` |
| `chore` | Công việc maintenance | `chore/update-dependencies` |
| `hotfix` | Sửa lỗi khẩn cấp | `hotfix/security-token-leak` |

### 1.3 Rebase thường xuyên

Tránh branch tồn tại lâu. Rebase hoặc merge `main` thường xuyên để giảm conflicts.

---

## §2 — Bảo vệ Branch `main`

| Quy tắc | Mô tả |
|---------|-------|
| **Bắt buộc PR** | Không được push trực tiếp vào `main` |
| **CI phải pass** | Build, test, lint phải thành công |
| **Code review** | Tối thiểu 1-2 reviewers approve |
| **Branch protection** | Bật branch protection rules trên GitHub/GitLab |

---

## §3 — Pull Request (PR)

### 3.1 Nguyên tắc

- **Nhỏ và tập trung**: Mỗi PR chỉ giải quyết 1 vấn đề
- **Mô tả rõ ràng**: Intent, test impact, migration steps (nếu có)
- **Link issue/ticket**: Tham chiếu đến issue liên quan

### 3.2 PR Template

```markdown
## Mô tả
[Mô tả ngắn gọn thay đổi]

## Loại thay đổi
- [ ] Feature mới
- [ ] Bug fix
- [ ] Refactor
- [ ] Breaking change

## Checklist
- [ ] Tests đã pass
- [ ] Code đã được review
- [ ] Documentation đã cập nhật (nếu cần)
```

---

## §4 — Commit Messages

### 4.1 Format

```
type(scope): mô tả ngắn gọn
```

### 4.2 Các loại commit

| Type | Khi nào dùng | Ví dụ |
|------|--------------|-------|
| `feat` | Thêm tính năng | `feat(auth): add token refresh` |
| `fix` | Sửa bug | `fix(upload): handle empty file` |
| `refactor` | Cải thiện code | `refactor(asset): extract validator` |
| `docs` | Cập nhật tài liệu | `docs(readme): update setup guide` |
| `test` | Thêm/sửa test | `test(auth): add login tests` |
| `chore` | Maintenance | `chore(deps): update packages` |

### 4.3 Git History

- **Squash** hoặc **rebase** để giữ history linear (tuỳ policy team)
- Dùng `--no-ff` merge nếu team muốn explicit merge commits

---

## §5 — Releases

| Bước | Mô tả |
|------|-------|
| **Tagging** | Dùng semantic versioning: `v1.2.3` |
| **Release notes** | Tự động generate từ CHANGELOG.md |
| **CI/CD** | Prefer automated release sau khi merge vào `main` |

---

## §6 — CI/CD Pipeline

### 6.1 Yêu cầu bắt buộc cho PR

```
✓ Unit tests pass
✓ Integration tests pass (nếu có)
✓ Linting pass
✓ Build thành công
```

### 6.2 Pipeline nên có

- Static analysis (SonarQube, CodeQL)
- Security scanners (dependency vulnerabilities)
- Build artifacts
- Deploy preview (staging)

---

## §7 — Code Review

### 7.1 Phân công reviewer

- **Theo ownership**: Assign người phụ trách module liên quan
- **Rotation**: Luân phiên reviewers để tránh bottleneck

### 7.2 Review checklist

- [ ] Logic đúng và đầy đủ
- [ ] Security considerations
- [ ] API changes có migration notes
- [ ] Tests cover edge cases
- [ ] Performance acceptable

---

## §8 — Hotfixes

### 8.1 Quy trình

```
1. Tạo branch: hotfix/issue-description từ main (hoặc tag mới nhất)
2. Fix và test
3. Fast-track review (vẫn cần 1 reviewer)
4. Merge vào main
5. Backport vào release branches (nếu cần)
```

### 8.2 Khi nào dùng hotfix

- Security vulnerabilities
- Critical production bugs
- Data corruption issues

---

## §9 — Dọn dẹp

| Hành động | Tần suất |
|-----------|----------|
| **Xoá remote branch sau merge** | Mỗi PR |
| **Prune stale branches** | Hàng tuần |
| **Alert branches > 30 ngày** | Tự động |

---

## Tóm tắt Quick Reference

```
Branch naming:    {type}/{scope}-{short-desc}
Commit format:    type(scope): description
PR:               Small, focused, linked to issue
Main protection:  CI + 1-2 reviewers + no direct push
Cleanup:          Delete branch after merge
```

---

> **Ghi chú**: Điều chỉnh các threshold (số reviewers, required checks) theo quy mô team và mức độ risk của dự án.

---

> **Document End**
