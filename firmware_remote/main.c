#include "stm8l15x.h"
#define LOW_CONSUMPTION
#define AD7793_CON_CH1 0
#define AD7793_CON_CH2 1
#define AD7793_CON_CH3 2
#define AD7793_CON_CHAvdd 7
#define AD7793_CON_CHTemp 6

#define AD7793_CON_GAIN1 0<<8
#define AD7793_CON_GAIN2 1<<8
#define AD7793_CON_GAIN4 2<<8
#define AD7793_CON_GAIN8 3<<8
#define AD7793_CON_GAIN16 4<<8
#define AD7793_CON_GAIN32 5<<8
#define AD7793_CON_GAIN64 6<<8
#define AD7793_CON_GAIN128 7<<8

#define AD7793_CON_BIASOFF 0<<8
#define AD7793_CON_BIASIN1 0x40<<8
#define AD7793_CON_BIASIN2 0x80<<8

#define AD7793_CON_BURNOUT 0x20<<8

#define AD7793_CON_UNIPOLAR 0x10<<8

#define AD7793_CON_BOOST 0x08<<8

#define AD7793_CON_INTREF 0x80

#define AD7793_CON_BUF 0x10

#define CON_TC_MEASURE  AD7793_CON_CH1|AD7793_CON_GAIN64|AD7793_CON_BIASIN1|AD7793_CON_INTREF
#define CON_REF_MEASURE AD7793_CON_CH3|AD7793_CON_GAIN2|AD7793_CON_BIASIN1|AD7793_CON_INTREF|AD7793_CON_UNIPOLAR
#define CON_RTD_MEASURE AD7793_CON_CH2|AD7793_CON_GAIN2|AD7793_CON_BIASIN1|AD7793_CON_INTREF|AD7793_CON_UNIPOLAR
#define CON_Avdd_MEASURE AD7793_CON_CHAvdd|AD7793_CON_BIASIN1|AD7793_CON_INTREF|AD7793_CON_UNIPOLAR

#define AD7793_EX_0 0
#define AD7793_EX_10u 1
#define AD7793_EX_210u 2
#define AD7793_EX_1000u 3
#define AD7793_EX_DIRECT 0
#define AD7793_EX_REVERSE 4
#define AD7793_EX_OUT1 8
#define AD7793_EX_OUT2 0xC

unsigned char spibuf[4] = {0,0,0,0};
unsigned char spimlen = 255;
unsigned char spidir = 0;

  unsigned char avdd;
  unsigned long vtc;
  unsigned long current;
  unsigned long vrtd;

char intflag = 1;
#pragma vector = 17
__interrupt void adc_conversion_complete()
{
  EXTI->SR1 = 0x80;
  GPIOB->CR2 &=~ 0x80;
  intflag = 0;
  return;
}
#pragma vector = 28
__interrupt void spi()
{
  if(spidir)
  {
    SPI1->DR;
    if(spimlen !=255)
    {
      SPI1->DR = spibuf[spimlen];
      spimlen --;
    }
    
  }
  else
  {
    if(spimlen !=255)
    {
       spibuf[spimlen] = SPI1->DR;
       if(spimlen != 0)
       {
          SPI1->DR = 0xFF;
       }
       spimlen --;
    }
  }
  return;
}
void adc_qry(unsigned char write,unsigned char reg,unsigned char cread, unsigned char len)
{
  unsigned char b = 0;
  while(spimlen != 255);
  while(SPI1->SR & SPI_SR_RXNE);
  if (!write)
  {
    b |= 0x40;
  }
  if(cread)
  {
    b |= 0x04;
  }
  reg = reg % 8;
  b |= reg << 3;
  spimlen = len;
  spidir = write;
  SPI1->DR = b;
  return;
}
void adc_conf(unsigned long what)
{
  while(spimlen != 255);
  while(SPI1->SR & SPI_SR_RXNE);
  spibuf[0] = what & 0xFF;
  spibuf[1] = what >>8;
  adc_qry(1,2,0,1);
  return;  
  
}

void adc_exitation(unsigned char parms)
{
  while(spimlen != 255);
  while(SPI1->SR & SPI_SR_RXNE);
  spibuf[0] = parms & 0x0F;
  adc_qry(1,5,0,0);
  return;  
  
}
  
void adc_wait_DRDY()
{
  while(spimlen != 255);
  while(SPI1->SR & SPI_SR_RXNE);
  GPIOB->CR2 |= 0x80;
  intflag = 1;
  while(intflag)    
  {
#ifdef LOW_CONSUMPTION
    wfi();
#endif
  }  
  return;
}
void adc_selfcal(unsigned char fullscale)
{
  while(spimlen != 255);
  while(SPI1->SR & SPI_SR_RXNE);
  spibuf[1] = 0x80;
  spibuf[0] = 0x0F;
  adc_qry(1,1,0,1);
  adc_wait_DRDY();
  if(fullscale)
  {
    spibuf[1] = 0xA0;
    spibuf[0] = 0x0F;
    adc_qry(1,1,0,1);
    while(spimlen != 255);
    while(SPI1->SR & SPI_SR_RXNE);
    adc_wait_DRDY();
  }
}
void adc_run(unsigned char period)
{
  while(spimlen != 255);
  while(SPI1->SR & SPI_SR_RXNE);
  spibuf[1] = 0x00;
  spibuf[0] = 0x0F | period;
  adc_qry(1,1,0,1);
}  
void adc_reset()
{
  char i =0;
  
  SPI1->CR3 = 0;
  
  for(i=0; i<4;i++)
  {
    SPI1->DR = 0xFF;
    while(!(SPI1->SR & SPI_SR_RXNE));
    SPI1->DR;
  }
  SPI1->CR3 = SPI_CR3_RXIE;
  return;
}
unsigned long adc_get_data()
{
    unsigned long t;
    adc_wait_DRDY();
    adc_qry(0,3,0,3);
    while(spimlen != 255);
    while(SPI1->SR & SPI_SR_RXNE);
    t = (unsigned long)spibuf[0];
    t += (unsigned long)spibuf[1] << 8;
    t += (unsigned long)spibuf[2] << 16;
    return t;
}

unsigned char adc_supply()
{
  unsigned long v;
  adc_conf(CON_Avdd_MEASURE);
  adc_run(0x01);
  v = adc_get_data();
  v = v/23899 - 250;
  return v;
}
unsigned long rtd(char ref)
{
  if(ref)
  {
    adc_conf(CON_REF_MEASURE);
  }
  else
  {
    adc_conf(CON_RTD_MEASURE);
  }
  adc_run(0x0F);
  return adc_get_data();
}  

unsigned long tc()
{
    unsigned long u;
    adc_conf(CON_TC_MEASURE);
    u = adc_get_data();
    if(u > 0x800000)
    {
      u = u - 0x800000;
    }
    else
    {
      u = 0x800000 - u;
    }
    return u;
}
void transmit(unsigned char avddx, unsigned long t, unsigned long add)
{
  char i;
  USART1->CR2 &=~ USART_CR2_TEN;
  TIM4->ARR = 25;
  TIM4->CR1 |= TIM4_CR1_CEN;
  while(TIM4->CR1 & TIM4_CR1_CEN);
  USART1->CR2 |= USART_CR2_TEN;
  TIM4->ARR = 125;
  TIM4->CR1 |= TIM4_CR1_CEN;
  while(TIM4->CR1 & TIM4_CR1_CEN);

  USART1->DR = avddx;
  while(!(USART1->SR & USART_SR_TXE));
  for(i = 0; i < 3; i++)
  {
    USART1->DR = (t >> (8*(2-i))) & 0xFF;
    while(!(USART1->SR & USART_SR_TXE));
  }
  for(i = 0; i < 3; i++)
  {
    USART1->DR = (add >> (8*(2-i))) & 0xFF;
    while(!(USART1->SR & USART_SR_TXE));
  }
  return;  
}
int main( void )
{
  unsigned int count = 0;

  // CLK
  CLK->CKDIVR = 7;  // 16M/128 = 125 kHz
  CLK->PCKENR1 = CLK_PCKENR1_TIM4 | CLK_PCKENR1_SPI1 | CLK_PCKENR1_USART1 | CLK_PCKENR1_TIM4;
  CLK->PCKENR2 = 0;
  EXTI->CR2 = 0x80;
  // GPIO
  GPIOB->DDR = 0x77;
  GPIOB->CR1 = 0x77;
  GPIOB->CR2 = 0;
  GPIOD->DDR = 0x80;
  GPIOD->CR1 = 0x80;
  GPIOD->ODR = 0x80;
  GPIOC->DDR = 0x08;
  GPIOC->CR1 = 0x08;
  // SPI
  SPI1->CR2 = SPI_CR2_SSM | SPI_CR2_SSI;
  SPI1->CR1 = SPI_CR1_SPE | SPI_CR1_MSTR | SPI_CR1_CPOL | SPI_CR1_CPHA | 0x1 << 3; //baudrate
  SPI1->CR3 = SPI_CR3_RXIE;
  // USART
  USART1->CR1 = USART_CR1_PCEN | USART_CR1_M;
  USART1->CR2 = USART_CR2_TEN;
  USART1->BRR2 = 0x08;
  USART1->BRR1 = 0x06;  //1202 baud
  //TIM4
  TIM4->CR1 = TIM4_CR1_OPM;
  TIM4->PSCR = 5;
  TIM4->ARR = 125;
  // AD7793
  GPIOB->ODR |= 0x00;  // CS low
  enableInterrupts();
  adc_reset();
  adc_exitation(AD7793_EX_210u|AD7793_EX_DIRECT);
  adc_conf(CON_REF_MEASURE);
  adc_selfcal(1);
  adc_conf(CON_RTD_MEASURE);
  adc_selfcal(1);
  adc_conf(CON_TC_MEASURE);
  adc_selfcal(1);
  avdd = adc_supply();
  adc_run(0x0F);
  while(1)
  {
    tc();
    tc();
    vtc = tc();
    if(count % 2 == 0) 
    {
      rtd(1);
      rtd(1);
      current = rtd(1);
      transmit(~0x80 & avdd, vtc, current);
    }
    else
    {
      rtd(0);
      rtd(0);
      vrtd = rtd(0);
      transmit(0x80 | avdd, vtc, vrtd);
    }
    count ++;
    if(count == 100)
    {
      count = 0;
      avdd = adc_supply();
      tc();
    }
  }
}
