# Avalonia GUI

`OrderSystem.Desktop`은 주문 시스템을 데스크톱에서 조작하기 위한 Avalonia 셸이다.

현재 GUI는 API 요청 편집기가 아니라 작은 쇼핑몰 화면이다. 가상의 상품 카탈로그에서 상품과 수량을 고르고, 장바구니 수량을 조정한 뒤, 고객명·배송지·결제수단을 입력해 주문을 완료한다. 오른쪽 콘솔은 API 호출문 대신 쇼핑 중 발생한 장바구니, 결제, 배송 로그를 보여 준다.

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

메인 윈도우는 세 작업 영역으로 나뉜다.

- Product Catalog: 티셔츠, 머그, 노트, 토트백, 램프, 비누 세트 같은 가상 상품을 고른다.
- Shopping Cart / Checkout: 장바구니 수량을 조정하고 주문자, 배송지, 결제수단을 입력한다.
- Shopping Console: 상품 담기, 장바구니 변경, 주문 생성, 결제 승인, 배송지 확정 로그를 보여 준다.

주요 흐름은 다음과 같다.

- 상품 선택 후 수량을 정해 장바구니에 담는다.
- 장바구니에서 선택한 항목의 수량을 늘리거나 줄이고, 항목을 제거한다.
- 고객명, 배송지, 결제수단을 확인한 뒤 주문을 생성한다.
- 주문이 완료되면 재고가 차감되고 콘솔에 주문/결제/배송 로그가 남는다.

## Run

현재 개발 환경은 Rider 번들 .NET SDK를 사용한다.

```bash
/Applications/Rider.app/Contents/lib/ReSharperHost/macos-arm64/dotnet/dotnet run --project OrderSystem.Desktop/OrderSystem.Desktop.csproj
```

프로젝트 대상 프레임워크는 `net8.0`이지만, `Directory.Build.props`에서 런타임 롤포워드를 `Major`로 설정한다. `build/`로 재빌드한 뒤에는 `/Users/ymy/.dotnet`에 .NET 8 런타임이 없어도 로컬 .NET 9 런타임으로 앱 호스트를 직접 실행할 수 있다.

현재 미니 쇼핑몰 상태는 데스크톱 ViewModel 안에서 동작한다. 이후 실제 REST API 클라이언트로 연결할 때도 UI는 같은 쇼핑 흐름을 유지하고, 내부 구현만 API 호출로 바꾸는 방향을 유지한다.

## Verification

테스트는 Avalonia 프로젝트가 솔루션에 등록되어 있는지, 상품 카탈로그가 충분히 제공되는지, 선택 상품과 수량이 장바구니에 반영되는지, 장바구니 수량 조정과 제거가 되는지, 고객/배송/결제 입력으로 주문과 콘솔 로그가 생성되는지 확인한다.
