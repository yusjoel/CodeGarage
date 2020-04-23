# Observer Pattern  (观察者模式)
观察者模式主要是为了解耦主题(Subject)和观察者(Observer).
但对于有委托的语言来说, 没有必要额外实现这个模式.
现在一般用于消息中心, 也就是主题和观察者之间都不需要知道对方的存在.
只需要向消息中心注册和发送消息即可

## EventCenter
代码来自BehaviorDesigner.

使用Action, 最多支持3个参数.
注册的时候使用字符串类型.

优点是发送消息的时候写起来方便.
缺点是如果有多个订阅者, 而且需求的内容不一样的话, 就要增加参数个数.
增加参数个数就要修改所有引用到的地方.
如果想保留原来的代码不修改的话, 就要另外发送一次消息.
但万一新增数据类型和原来一致的话, 又要多出额外的操作.

## MessageBus
代码来自Apex Path (http://apexgametools.com/products/apex-path/)

观察者模式一般的实现机制是MessageType和MessageData分离的
messageType有时候是string型, 也可以是int型或者enum型.
MessageData要么是object型, 由订阅者自己来转类型, 或者是Dictionary这种通用结构.
这套代码则是直接使用Type型, 既当成MessageType, 也当成MessageData.
要说缺点的话就是一个消息必须对应一个类, 无法公用.

## NotificationCenter
代码来自地城战棋

注册时候使用字符串类型, 数据使用字典.
和其他两个比较起来, 有两个区别:
1. 多了sender, 可以只接收指定对象的消息,或者直接从发送者那里获取信息
2. 有多个观察者的时候可以中断向下传递
