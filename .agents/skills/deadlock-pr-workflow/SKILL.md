---
name: deadlock-pr-workflow
description: "DeadLock 프로젝트에서 `pr {타겟 브랜치}` 또는 PR 생성/제출/초안 작성 요청을 처리하는 GitHub PR 워크플로우입니다. 현재 브랜치의 커밋과 커밋할 변경 사항을 분석하고, .github/PULL_REQUEST_TEMPLATE.md 양식에 맞춰 PR 제목/본문 초안을 만들며, 반드시 사용자에게 작업 내용과 PR 본문을 먼저 보여주고 명시 승인 후에만 커밋, push, PR 생성을 수행합니다."
---

# DeadLock PR 워크플로우

## 트리거

사용자가 다음처럼 요청하면 이 스킬을 사용한다.

- `pr main`
- `pr develop`
- `pr {target-branch}`
- PR 보내줘
- PR 초안 만들어줘
- 현재 브랜치 작업으로 PR 올려줘

타겟 브랜치가 없으면 바로 실행하지 말고 타겟 브랜치를 물어본다.

## 절대 규칙

- PR을 바로 생성하지 않는다.
- push를 바로 하지 않는다.
- 커밋이 필요한 경우에도 바로 커밋하지 않는다.
- 항상 먼저 현재 작업 요약, 커밋 계획, PR 제목, PR 본문 초안을 사용자에게 보여준다.
- 사용자가 명시적으로 승인한 뒤에만 commit, push, PR 생성 작업을 수행한다.
- 승인 전에는 GitHub에 PR을 만들거나 원격 브랜치를 갱신하지 않는다.
- 사용자가 수정을 요청하면 초안을 고친 뒤 다시 승인을 받는다.

## 정보 수집

`pr {target-branch}` 요청을 받으면 먼저 다음을 확인한다.

```bash
git branch --show-current
git status --short
git remote -v
git fetch origin {target-branch}
git log --oneline origin/{target-branch}..HEAD
git diff --stat origin/{target-branch}...HEAD
git diff --name-status origin/{target-branch}...HEAD
```

커밋되지 않은 변경이 있으면 다음도 확인한다.

```bash
git diff --stat
git diff --name-status
git diff --cached --stat
git diff --cached --name-status
```

대용량 에셋이나 바이너리 변경이 많으면 diff 본문 전체를 읽으려 하지 말고 파일명, 크기, 변경 유형 중심으로 요약한다.

## PR 템플릿

PR 본문은 반드시 다음 파일을 읽어서 작성한다.

```text
D:/DeadLock/.github/PULL_REQUEST_TEMPLATE.md
```

PowerShell 콘솔에서 한글이 깨져 보일 수 있으므로, 가능하면 UTF-8 텍스트로 읽고 원본 heading 구조를 보존한다.

템플릿에 다음 항목이 있으면 자동으로 채운다.

- PR 작성 전 체크리스트
- 작업 내용
- 예상 리뷰 시간
- 이슈 번호
- 리뷰 요구사항

확인하지 못한 체크리스트는 임의로 체크하지 않는다. 검증을 하지 않았으면 검증하지 않았다고 적는다.

## 초안 작성 기준

초안에는 다음을 포함한다.

- 현재 브랜치
- 타겟 브랜치
- 원격 저장소
- 타겟 브랜치 대비 포함될 커밋 목록
- 커밋되지 않은 변경 요약
- 커밋이 필요한지 여부
- 제안 커밋 메시지
- 제안 PR 제목
- 템플릿을 채운 PR 본문
- 검증 실행 여부
- 승인 후 수행할 정확한 명령 목록

PR 제목은 변경 목적이 드러나게 한국어로 작성한다. 이미 좋은 커밋 메시지나 브랜치명이 있으면 참고한다.

## 커밋 정책

커밋되지 않은 변경이 없으면 기존 커밋만으로 PR 초안을 만든다.

커밋되지 않은 변경이 있으면:

1. 변경 파일을 요약한다.
2. 커밋할 파일 범위를 제안한다.
3. 커밋 메시지를 제안한다.
4. 사용자 승인 전에는 stage/commit 하지 않는다.

사용자가 승인하면 제안한 범위만 stage/commit 한다. 관련 없는 사용자 변경은 포함하지 않는다.

## Push와 PR 생성

사용자 승인 뒤에만 다음을 수행한다.

1. 필요한 경우 commit.
2. 현재 브랜치를 origin에 push.
3. GitHub app 또는 `gh pr create`를 사용해 PR 생성.
4. 생성 결과 URL을 보고.

가능하면 GitHub connector를 우선 사용하고, 부족하면 `gh` CLI를 사용한다.

PR 생성 명령 예시:

```bash
gh pr create --base {target-branch} --head {current-branch} --title "{title}" --body-file "{body-file}"
```

PR이 이미 있으면 새 PR을 만들지 말고 기존 PR URL을 확인한 뒤, 업데이트할지 사용자에게 묻는다.

## 최종 보고

commit, push, PR 생성을 실제로 수행한 경우 Codex 앱 지침에 따라 최종 응답에 필요한 git directive를 포함한다.

검증을 실행하지 않았으면 명확히 말한다. Unity 검증은 `deadlock-unity-validation-manual` 요청이 있을 때만 수행한다.

