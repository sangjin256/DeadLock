---
name: deadlock-unity-validation-manual
description: "DeadLock의 수동 전용 Unity 검증 절차입니다. 사용자가 검증, 컴파일, Unity 테스트 실행, 콘솔 로그 확인, MCP Unity 상태 확인, 최종 확인을 명시적으로 요청한 경우에만 사용합니다. 일반 코드 수정, 문서 수정, 스킬 수정, 아키텍처 작업 뒤에는 자동으로 실행하지 않습니다."
---

# DeadLock Unity 수동 검증

## 트리거 정책

다음처럼 명시적인 검증 요청이 있을 때만 사용한다.

- "validate"
- "compile check"
- "run Unity tests"
- "check console errors"
- "검증해줘"
- "컴파일 확인해줘"
- "Unity 테스트 돌려줘"
- "콘솔 에러 확인해줘"
- "최종 검증해줘"

일반 구현 중에는 Unity 리컴파일, PlayMode 테스트, EditMode 테스트, MCP Unity 호출, 콘솔 로그 polling을 실행하지 않는다.

## 일반 작업 보고

일반 구현이 끝났을 때는 다음을 보고한다.

- Unity 검증을 실행하지 않았다는 점.
- 변경 사항이 Unity 검증을 필요로 할 가능성.
- 추천 수동 검증 절차.

Unity가 열려 있지 않거나 접근할 수 없었는데 검증 성공처럼 말하지 않는다.

## 수동 검증 절차

사용자가 명시적으로 요청한 경우:

1. 변경 파일이 Unity 컴파일을 필요로 하는지 판단한다.
2. Unity MCP를 사용할 수 있고 Unity가 열려 있으면 가장 작은 관련 검증을 실행한다.
3. 컴파일 또는 테스트 뒤 콘솔 에러를 확인한다.
4. 관련 EditMode 테스트가 있으면 집중 실행한다.
5. PlayMode 테스트는 요청이 있거나 런타임 상호작용을 직접 바꾼 경우에만 실행한다.
6. 실패는 실행 가능한 메시지로 보고하고, 수행하지 못한 검증을 숨기지 않는다.

## 문서와 스킬 변경

Markdown, TOML, 스킬 metadata 같은 비 Unity 프로젝트 파일 변경은 Unity 컴파일을 하지 않는다. 구조와 문법만 확인한다.

