package org.acme;

import io.quarkus.runtime.annotations.RegisterForReflection;

@RegisterForReflection
public class LimiteSaldo {

    public Integer limite;
    public Integer saldo;

    public LimiteSaldo(Integer saldo) {
        this.saldo = saldo;
    }

    public LimiteSaldo(Integer saldo, Integer limite) {
        this.limite = limite;
        this.saldo = saldo;
    }

}
