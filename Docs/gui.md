# Avalonia GUI

`OrderSystem.Desktop`은 주문 시스템을 데스크톱에서 조작하기 위한 Avalonia 셸이다.

현재 GUI는 단순 POST 요청 로그가 아니라 출력 콘솔 안에 작은 쇼핑몰 상태를 표시한다. 카탈로그, 장바구니, 주문, 결제, 재고 화면을 같은 콘솔에서 전환하며, 선택한 액션을 실행하면 장바구니 수량, 주문 상태, 결제 상태, 재고 수량이 즉시 바뀐다. 액션 프리셋은 기본 요청 예시를 채워 줄 뿐이며, 사용자는 API base URL, method, path, JSON body를 직접 바꿔 어떤 요청을 보낼지 결정할 수 있다.

`OrderSystem.Desktop`은 `OrderSystem.sln`의 첫 프로젝트로 배치되어 솔루션 순서에서 시작 프로젝트를 추론하는 IDE에서 GUI 실행 후보가 된다.

## Project

```text
OrderSystem.Desktop
├── App.axaml
├── Program.cs
├── ViewModels
│   ├── MainWindowViewModel.cs
│   └── RelayCommand.cs
└── Views
    └── MainWindow.axaml
```

## View

메인 윈도우는 두 작업 영역으로 나뉜다.

- Operation Console: `GET`, `POST`, `PATCH` 액션을 선택한 뒤 API base URL, method, path, 요청 payload를 편집한다.
- Output Console: 카탈로그, 장바구니, 주문, 결제, 재고, 최근 활동을 미니 쇼핑몰 화면처럼 보여 준다.

지원하는 액션은 다음과 같다.

- `GET /api/products`: 카탈로그 보기
- `POST /shop/cart/items`: 장바구니 담기
- `GET /shop/cart`: 장바구니 보기
- `POST /api/orders`: 장바구니 주문 생성
- `GET /api/orders`: 주문 보기
- `PATCH /api/orders/{latest}/status`: 최신 주문 확인 처리
- `POST /api/orders/{latest}/payments`: 카드 결제 기록
- `POST /api/orders/{latest}/cancel`: 최신 주문 취소
- `GET /api/inventory`: 재고 보기

## Run

현재 개발 환경은 Rider 번들 .NET SDK를 사용한다.

```bash
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet run --project OrderSystem.Desktop/OrderSystem.Desktop.csproj
```

프로젝트 대상 프레임워크는 `net8.0`이지만, `Directory.Build.props`에서 런타임 롤포워드를 `Major`로 설정한다. `build/`로 재빌드한 뒤에는 `/Users/ymy/.dotnet`에 .NET 8 런타임이 없어도 로컬 .NET 9 런타임으로 앱 호스트를 직접 실행할 수 있다.

데스크톱 셸의 기본 API 주소는 `http://localhost:5000`이다. 상단 입력칸에서 실행 대상 API 주소를 바꿀 수 있고, 좌측 요청 구성 영역에서 method/path/body를 조정할 수 있다. 현재 미니 쇼핑몰 상태는 데스크톱 ViewModel 안에서 동작하며, 이후 같은 요청 구성 값을 실제 REST API 클라이언트로 연결할 수 있다.

## Verification

테스트는 Avalonia 프로젝트가 솔루션에 등록되어 있는지, GUI가 `GET`/`POST`/`PATCH` 주문 액션을 제공하는지, 선택 변경과 장바구니/주문 실행이 출력 콘솔 상태를 갱신하는지 확인한다. 또한 사용자가 JSON body의 `sku`, `quantity`, `customerId`, `status`, `paymentMethod` 값을 바꿨을 때 출력 콘솔의 장바구니, 주문, 결제 상태가 그 값으로 바뀌는지 확인한다.
