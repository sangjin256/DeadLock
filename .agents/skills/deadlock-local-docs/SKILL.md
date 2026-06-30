---
name: deadlock-local-docs
description: "DeadLock 프로젝트의 로컬 문서 작업 흐름입니다. Docs/ 아래의 기획, 아키텍처, 구현 현황, 비주얼 방향, 모바일 이식, 결정 기록 문서를 생성/수정/참조할 때 사용합니다. 이 프로젝트의 계획 문서는 Notion 동기화나 외부 업로드를 사용하지 않습니다."
---

# DeadLock 로컬 문서

## 문서 출처

프로젝트 계획의 기준은 `Docs/` 아래 로컬 파일이다.

- `Docs/DeadLock_GameDesign.md`
- `Docs/DeadLock_Architecture.md`
- `Docs/DeadLock_DevelopmentStatus.md`
- `Docs/DeadLock_VisualDirection.md`
- `Docs/DeadLock_MobilePorting.md`

필요한 문서가 없을 때만 새로 만든다.

## 작업 흐름

1. 아키텍처, 게임플레이, 비주얼, 모바일 이식 작업 전에는 관련 로컬 문서가 있으면 읽는다.
2. 설계 결정은 로컬 Markdown 파일에 기록한다.
3. Notion, Notion sync, 원격 문서 발행, 자동 업로드는 사용하지 않는다.
4. 큰 작업 뒤 다음 세션에 도움이 되면 `DeadLock_DevelopmentStatus.md`를 갱신한다.
5. 문서는 간결하고 구현에 도움이 되게 작성한다.

## 기록할 것

- 현재 목표 아키텍처와 마이그레이션 상태.
- DeadLock 고유 도메인 용어.
- 모바일 이식 제약.
- 비주얼 방향과 사용 가능한 도구.
- 알려진 검증 공백.
- 다음 에이전트가 다시 추론하지 않아도 되는 결정.

## 검증 정책

문서만 바뀐 경우 Unity 컴파일은 필요 없다. 사용자가 명시적으로 Unity 검증을 요청하지 않는 한 정적 파일 검토만 수행한다.

